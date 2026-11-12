using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaresApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAzureSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LARES_CATEGORIA",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    descricao = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    prazo_padrao_dias = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LARES_CATEGORIA", x => x.id);
                    table.CheckConstraint("CK_LARES_CATEGORIA_PRAZO", "[prazo_padrao_dias] > 0");
                });

            migrationBuilder.CreateTable(
                name: "LARES_CHAVE_IDEMPOTENCIA",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    hash_requisicao = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    status_code = table.Column<int>(type: "int", nullable: true),
                    resposta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    localizacao = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LARES_CHAVE_IDEMPOTENCIA", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "LARES_USUARIO",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    senha_hash = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    papel = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    data_cadastro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LARES_USUARIO", x => x.id);
                    table.CheckConstraint("CK_LARES_USUARIO_PAPEL", "[papel] IN ('CIDADAO', 'GESTOR')");
                });

            migrationBuilder.CreateTable(
                name: "LARES_OCORRENCIA",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    usuario_id = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    usuario_nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    categoria_id = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: true),
                    categoria_codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    categoria_nome = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    bairro = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    bairro_busca = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    status = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    prioridade = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: true),
                    prioridade_ordem = table.Column<int>(type: "int", nullable: false),
                    prazo_estimado_dias = table.Column<int>(type: "int", nullable: true),
                    resumo_ia = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    mensagem_cidadao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    data_abertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LARES_OCORRENCIA", x => x.id);
                    table.CheckConstraint("CK_LARES_OCORRENCIA_PRIORIDADE", "[prioridade] IS NULL OR [prioridade] IN ('BAIXA', 'MEDIA', 'ALTA')");
                    table.CheckConstraint("CK_LARES_OCORRENCIA_STATUS", "[status] IN ('RECEBIDA', 'TRIADA', 'EM_ANDAMENTO', 'RESOLVIDA')");
                    table.ForeignKey(
                        name: "FK_LARES_OCORRENCIA_LARES_CATEGORIA_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "LARES_CATEGORIA",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LARES_OCORRENCIA_LARES_USUARIO_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "LARES_USUARIO",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LARES_HISTORICO_STATUS",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    status_anterior = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    status_novo = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    comentario = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    usuario_id = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: true),
                    usuario_nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    data_alteracao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ocorrencia_id = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LARES_HISTORICO_STATUS", x => x.id);
                    table.ForeignKey(
                        name: "FK_LARES_HISTORICO_STATUS_LARES_OCORRENCIA_ocorrencia_id",
                        column: x => x.ocorrencia_id,
                        principalTable: "LARES_OCORRENCIA",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LARES_HISTORICO_STATUS_LARES_USUARIO_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "LARES_USUARIO",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LARES_CATEGORIA_codigo",
                table: "LARES_CATEGORIA",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LARES_HISTORICO_STATUS_ocorrencia_id",
                table: "LARES_HISTORICO_STATUS",
                column: "ocorrencia_id");

            migrationBuilder.CreateIndex(
                name: "IX_LARES_HISTORICO_STATUS_usuario_id",
                table: "LARES_HISTORICO_STATUS",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_LARES_OCORRENCIA_categoria_id",
                table: "LARES_OCORRENCIA",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_LARES_OCORRENCIA_usuario_id",
                table: "LARES_OCORRENCIA",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_LARES_USUARIO_email",
                table: "LARES_USUARIO",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LARES_CHAVE_IDEMPOTENCIA");

            migrationBuilder.DropTable(
                name: "LARES_HISTORICO_STATUS");

            migrationBuilder.DropTable(
                name: "LARES_OCORRENCIA");

            migrationBuilder.DropTable(
                name: "LARES_CATEGORIA");

            migrationBuilder.DropTable(
                name: "LARES_USUARIO");
        }
    }
}
