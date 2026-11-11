using LaresApi.Models;
using LaresApi.Services;

namespace LaresApi.Tests.Fakes
{
    public class FakeIdempotenciaStore : IIdempotenciaStore
    {
        public Dictionary<string, ChaveIdempotencia> Registros { get; } = new();

        public Task<ChaveIdempotencia?> ObterAsync(string chave, CancellationToken ct) =>
            Task.FromResult(Registros.TryGetValue(chave, out var r) ? r : null);

        public Task<bool> TentarReservarAsync(ChaveIdempotencia registro, CancellationToken ct) =>
            Task.FromResult(Registros.TryAdd(registro.Id, registro));

        public Task ConcluirAsync(string chave, int statusCode, string resposta, string? localizacao, CancellationToken ct)
        {
            var r = Registros[chave];
            r.StatusCode = statusCode;
            r.Resposta = resposta;
            r.Localizacao = localizacao;
            return Task.CompletedTask;
        }

        public Task RemoverAsync(string chave, CancellationToken ct)
        {
            Registros.Remove(chave);
            return Task.CompletedTask;
        }
    }
}
