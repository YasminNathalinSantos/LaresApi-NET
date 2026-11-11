using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LaresApi.OpenApi
{
    public sealed class InfoEJwtTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            document.Info = new OpenApiInfo
            {
                Title = "Lares API (.NET)",
                Version = "v1",
                Description = "CitizenTech: o cidadão registra problemas urbanos e a prefeitura prioriza e acompanha o atendimento. " +
                              "Faça login em /auth/login, copie o token e clique em Authorize."
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Cole apenas o token (sem a palavra Bearer)."
                }
            };

            foreach (var caminho in document.Paths.Values)
            {
                if (caminho.Operations is null) continue;

                foreach (var operacao in caminho.Operations.Values)
                {
                    operacao.Security ??= [];
                    operacao.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    });
                }
            }

            return Task.CompletedTask;
        }
    }
}
