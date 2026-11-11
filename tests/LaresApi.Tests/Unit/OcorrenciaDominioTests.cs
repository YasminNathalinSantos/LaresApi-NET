using LaresApi.Domain;
using LaresApi.Models;

namespace LaresApi.Tests.Unit
{
    public class OcorrenciaDominioTests
    {
        private static readonly DateTime Agora = new(2026, 11, 10, 9, 15, 0);
        private static readonly Usuario Cidadao = new() { Id = "6730a1b2c3d4e5f6a7b8c9d1", Nome = "Ana", Email = "ana@email.com", SenhaHash = "x", Papel = Papeis.Cidadao };
        private static readonly Usuario Gestor = new() { Id = "6730a1b2c3d4e5f6a7b8c9d2", Nome = "Gestor", Email = "g@lares.com", SenhaHash = "x", Papel = Papeis.Gestor };
        private static readonly Categoria Buraco = new() { Id = "6730a1b2c3d4e5f6a7b8c9d3", Codigo = "BURACO", Nome = "Buraco na via", PrazoPadraoDias = 15 };

        private static Ocorrencia NovaOcorrencia() =>
            Ocorrencia.Abrir(Cidadao, "Buraco na frente da escola", "Vila Mariana", null, null, Agora);

        [Fact]
        public void Abrir_NasceRecebida_EGravaHistorico()
        {
            var o = NovaOcorrencia();

            Assert.Equal(StatusOcorrencia.Recebida, o.Status);
            var linha = Assert.Single(o.Historico);
            Assert.Null(linha.StatusAnterior);
            Assert.Equal(StatusOcorrencia.Recebida, linha.StatusNovo);
        }

        [Fact]
        public void Triagem_SemPrazo_UsaPrazoPadraoDaCategoria()
        {
            var o = NovaOcorrencia();

            o.AlterarStatus(StatusOcorrencia.Triada, Gestor, null, Prioridades.Alta, null, Buraco, Agora);

            Assert.Equal(StatusOcorrencia.Triada, o.Status);
            Assert.Equal(Prioridades.Alta, o.Prioridade);
            Assert.Equal(15, o.PrazoEstimadoDias);
            Assert.Equal(2, o.Historico.Count);
        }

        [Fact]
        public void Triagem_ComPrazo_UsaPrazoDoGestor()
        {
            var o = NovaOcorrencia();

            o.AlterarStatus(StatusOcorrencia.Triada, Gestor, null, Prioridades.Media, 4, Buraco, Agora);

            Assert.Equal(4, o.PrazoEstimadoDias);
        }

        [Fact]
        public void Triagem_SemPrioridade_LancaValidacao()
        {
            var o = NovaOcorrencia();

            var ex = Assert.Throws<ValidacaoException>(() =>
                o.AlterarStatus(StatusOcorrencia.Triada, Gestor, null, null, null, Buraco, Agora));

            Assert.Contains("prioridade", ex.Campos!.Keys);
            Assert.Equal(StatusOcorrencia.Recebida, o.Status);
        }

        [Fact]
        public void Triagem_SemCategoria_LancaValidacao()
        {
            var o = NovaOcorrencia();

            var ex = Assert.Throws<ValidacaoException>(() =>
                o.AlterarStatus(StatusOcorrencia.Triada, Gestor, null, Prioridades.Alta, null, null, Agora));

            Assert.Contains("categoria", ex.Campos!.Keys);
        }

        [Fact]
        public void TransicaoInvalida_LancaConflito()
        {
            var o = NovaOcorrencia();

            Assert.Throws<ConflitoException>(() =>
                o.AlterarStatus(StatusOcorrencia.Resolvida, Gestor, null, Prioridades.Alta, null, Buraco, Agora));
        }

        [Fact]
        public void FluxoCompleto_AteResolvida_GravaUmaLinhaPorMudanca()
        {
            var o = NovaOcorrencia();

            o.AlterarStatus(StatusOcorrencia.Triada, Gestor, null, Prioridades.Alta, null, Buraco, Agora);
            o.AlterarStatus(StatusOcorrencia.EmAndamento, Gestor, "Equipe enviada", null, null, null, Agora);
            o.AlterarStatus(StatusOcorrencia.Resolvida, Gestor, "Buraco tapado", null, null, null, Agora);

            Assert.Equal(StatusOcorrencia.Resolvida, o.Status);
            Assert.Equal(4, o.Historico.Count);
            Assert.Equal(Prioridades.Alta, o.Prioridade);
        }

        [Fact]
        public void Cidadao_EditaDepoisDeEmAndamento_LancaConflito()
        {
            var o = NovaOcorrencia();
            o.AlterarStatus(StatusOcorrencia.EmAndamento, Gestor, null, Prioridades.Baixa, null, Buraco, Agora);

            Assert.Throws<ConflitoException>(() =>
                o.AtualizarDados("Nova descrição", "Moema", null, null, Agora));
        }

        [Fact]
        public void Cidadao_EditaTriada_Funciona()
        {
            var o = NovaOcorrencia();
            o.AlterarStatus(StatusOcorrencia.Triada, Gestor, null, Prioridades.Baixa, null, Buraco, Agora);

            o.AtualizarDados("Nova descrição", "Moema", null, null, Agora);

            Assert.Equal("Moema", o.Bairro);
        }
    }
}
