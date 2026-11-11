using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LaresApi.Data;
using LaresApi.Domain;
using LaresApi.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace LaresApi.Services
{
    public record RespostaOperacao(int StatusCode, object Corpo, string? Localizacao);

    public record ResultadoIdempotente(int StatusCode, string CorpoJson, string? Localizacao, bool Repetida);

    public interface IIdempotenciaStore
    {
        Task<ChaveIdempotencia?> ObterAsync(string chave, CancellationToken ct);
        Task<bool> TentarReservarAsync(ChaveIdempotencia registro, CancellationToken ct);
        Task ConcluirAsync(string chave, int statusCode, string resposta, string? localizacao, CancellationToken ct);
        Task RemoverAsync(string chave, CancellationToken ct);
    }

    public class IdempotenciaService(IIdempotenciaStore store, TimeProvider tempo)
    {
        public const int TamanhoMaximoChave = 100;
        public static readonly TimeSpan Validade = TimeSpan.FromHours(24);

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public async Task<ResultadoIdempotente> ExecutarAsync(string chaveCliente, string usuarioId, object requisicao,
            Func<Task<RespostaOperacao>> operacao, CancellationToken ct)
        {
            ValidarChave(chaveCliente);

            var chave = Sha256($"{usuarioId}:{chaveCliente.Trim()}");
            var hashRequisicao = Sha256(JsonSerializer.Serialize(requisicao, requisicao.GetType(), Json));

            for (var tentativa = 0; tentativa < 2; tentativa++)
            {
                var existente = await store.ObterAsync(chave, ct);

                if (existente is not null && existente.DataCriacao < DataHora.AgoraUtc(tempo) - Validade)
                {
                    await store.RemoverAsync(chave, ct);
                    existente = null;
                }

                if (existente is not null)
                    return Repetir(existente, hashRequisicao);

                var reservado = await store.TentarReservarAsync(new ChaveIdempotencia
                {
                    Id = chave,
                    HashRequisicao = hashRequisicao,
                    DataCriacao = DataHora.AgoraUtc(tempo)
                }, ct);

                if (!reservado)
                    continue;

                try
                {
                    var resposta = await operacao();
                    var corpoJson = JsonSerializer.Serialize(resposta.Corpo, resposta.Corpo.GetType(), Json);
                    await store.ConcluirAsync(chave, resposta.StatusCode, corpoJson, resposta.Localizacao, ct);
                    return new ResultadoIdempotente(resposta.StatusCode, corpoJson, resposta.Localizacao, Repetida: false);
                }
                catch
                {
                    await store.RemoverAsync(chave, CancellationToken.None);
                    throw;
                }
            }

            throw new ConflitoException("Já existe uma requisição com esta Idempotency-Key em processamento.");
        }

        private static ResultadoIdempotente Repetir(ChaveIdempotencia existente, string hashRequisicao)
        {
            if (existente.HashRequisicao != hashRequisicao)
                throw new IdempotenciaConflitoException(
                    "Esta Idempotency-Key já foi usada com outro corpo de requisição. Gere uma nova chave.");

            if (existente.StatusCode is null || existente.Resposta is null)
                throw new ConflitoException("A requisição original com esta Idempotency-Key ainda está em processamento.");

            return new ResultadoIdempotente(existente.StatusCode.Value, existente.Resposta, existente.Localizacao, Repetida: true);
        }

        private static void ValidarChave(string chaveCliente)
        {
            if (string.IsNullOrWhiteSpace(chaveCliente) || chaveCliente.Trim().Length > TamanhoMaximoChave)
                throw new ValidacaoException(new Dictionary<string, string>
                {
                    ["Idempotency-Key"] = $"deve ter entre 1 e {TamanhoMaximoChave} caracteres"
                });
        }

        private static string Sha256(string texto) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }

    public class EfIdempotenciaStore(LaresDbContext db) : IIdempotenciaStore
    {
        public Task<ChaveIdempotencia?> ObterAsync(string chave, CancellationToken ct) =>
            db.ChavesIdempotencia.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chave, ct);

        public async Task<bool> TentarReservarAsync(ChaveIdempotencia registro, CancellationToken ct)
        {
            db.ChavesIdempotencia.Add(registro);
            try
            {
                await db.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex) when (ex is DbUpdateException or MongoException or InvalidOperationException or ArgumentException)
            {
                db.Entry(registro).State = EntityState.Detached;
                return false;
            }
        }

        public async Task ConcluirAsync(string chave, int statusCode, string resposta, string? localizacao, CancellationToken ct)
        {
            var registro = await db.ChavesIdempotencia.FirstOrDefaultAsync(c => c.Id == chave, ct);
            if (registro is null) return;

            registro.StatusCode = statusCode;
            registro.Resposta = resposta;
            registro.Localizacao = localizacao;
            await db.SaveChangesAsync(ct);
        }

        public async Task RemoverAsync(string chave, CancellationToken ct)
        {
            db.ChangeTracker.Clear();
            var registro = await db.ChavesIdempotencia.FirstOrDefaultAsync(c => c.Id == chave, ct);
            if (registro is null) return;

            db.ChavesIdempotencia.Remove(registro);
            await db.SaveChangesAsync(ct);
        }
    }
}
