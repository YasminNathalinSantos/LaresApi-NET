IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [LARES_CATEGORIA] (
    [id] varchar(24) NOT NULL,
    [codigo] varchar(20) NOT NULL,
    [nome] nvarchar(60) NOT NULL,
    [descricao] nvarchar(255) NULL,
    [prazo_padrao_dias] int NOT NULL,
    CONSTRAINT [PK_LARES_CATEGORIA] PRIMARY KEY ([id]),
    CONSTRAINT [CK_LARES_CATEGORIA_PRAZO] CHECK ([prazo_padrao_dias] > 0)
);

CREATE TABLE [LARES_CHAVE_IDEMPOTENCIA] (
    [id] varchar(64) NOT NULL,
    [hash_requisicao] varchar(64) NOT NULL,
    [status_code] int NULL,
    [resposta] nvarchar(max) NULL,
    [localizacao] nvarchar(2048) NULL,
    [data_criacao] datetime2 NOT NULL,
    CONSTRAINT [PK_LARES_CHAVE_IDEMPOTENCIA] PRIMARY KEY ([id])
);

CREATE TABLE [LARES_USUARIO] (
    [id] varchar(24) NOT NULL,
    [nome] nvarchar(100) NOT NULL,
    [email] nvarchar(150) NOT NULL,
    [senha_hash] varchar(100) NOT NULL,
    [papel] varchar(10) NOT NULL,
    [data_cadastro] datetime2 NOT NULL,
    CONSTRAINT [PK_LARES_USUARIO] PRIMARY KEY ([id]),
    CONSTRAINT [CK_LARES_USUARIO_PAPEL] CHECK ([papel] IN ('CIDADAO', 'GESTOR'))
);

CREATE TABLE [LARES_OCORRENCIA] (
    [id] varchar(24) NOT NULL,
    [usuario_id] varchar(24) NOT NULL,
    [usuario_nome] nvarchar(100) NOT NULL,
    [categoria_id] varchar(24) NULL,
    [categoria_codigo] varchar(20) NULL,
    [categoria_nome] nvarchar(60) NULL,
    [descricao] nvarchar(500) NOT NULL,
    [bairro] nvarchar(80) NOT NULL,
    [bairro_busca] nvarchar(80) NOT NULL,
    [latitude] decimal(9,6) NULL,
    [longitude] decimal(9,6) NULL,
    [status] varchar(15) NOT NULL,
    [prioridade] varchar(5) NULL,
    [prioridade_ordem] int NOT NULL,
    [prazo_estimado_dias] int NULL,
    [resumo_ia] nvarchar(1000) NULL,
    [mensagem_cidadao] nvarchar(1000) NULL,
    [data_abertura] datetime2 NOT NULL,
    [data_atualizacao] datetime2 NOT NULL,
    CONSTRAINT [PK_LARES_OCORRENCIA] PRIMARY KEY ([id]),
    CONSTRAINT [CK_LARES_OCORRENCIA_PRIORIDADE] CHECK ([prioridade] IS NULL OR [prioridade] IN ('BAIXA', 'MEDIA', 'ALTA')),
    CONSTRAINT [CK_LARES_OCORRENCIA_STATUS] CHECK ([status] IN ('RECEBIDA', 'TRIADA', 'EM_ANDAMENTO', 'RESOLVIDA')),
    CONSTRAINT [FK_LARES_OCORRENCIA_LARES_CATEGORIA_categoria_id] FOREIGN KEY ([categoria_id]) REFERENCES [LARES_CATEGORIA] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LARES_OCORRENCIA_LARES_USUARIO_usuario_id] FOREIGN KEY ([usuario_id]) REFERENCES [LARES_USUARIO] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [LARES_HISTORICO_STATUS] (
    [id] int NOT NULL IDENTITY,
    [status_anterior] varchar(15) NULL,
    [status_novo] varchar(15) NOT NULL,
    [comentario] nvarchar(255) NULL,
    [usuario_id] varchar(24) NULL,
    [usuario_nome] nvarchar(100) NULL,
    [data_alteracao] datetime2 NOT NULL,
    [ocorrencia_id] varchar(24) NOT NULL,
    CONSTRAINT [PK_LARES_HISTORICO_STATUS] PRIMARY KEY ([id]),
    CONSTRAINT [FK_LARES_HISTORICO_STATUS_LARES_OCORRENCIA_ocorrencia_id] FOREIGN KEY ([ocorrencia_id]) REFERENCES [LARES_OCORRENCIA] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LARES_HISTORICO_STATUS_LARES_USUARIO_usuario_id] FOREIGN KEY ([usuario_id]) REFERENCES [LARES_USUARIO] ([id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_LARES_CATEGORIA_codigo] ON [LARES_CATEGORIA] ([codigo]);

CREATE INDEX [IX_LARES_HISTORICO_STATUS_ocorrencia_id] ON [LARES_HISTORICO_STATUS] ([ocorrencia_id]);

CREATE INDEX [IX_LARES_HISTORICO_STATUS_usuario_id] ON [LARES_HISTORICO_STATUS] ([usuario_id]);

CREATE INDEX [IX_LARES_OCORRENCIA_categoria_id] ON [LARES_OCORRENCIA] ([categoria_id]);

CREATE INDEX [IX_LARES_OCORRENCIA_usuario_id] ON [LARES_OCORRENCIA] ([usuario_id]);

CREATE UNIQUE INDEX [IX_LARES_USUARIO_email] ON [LARES_USUARIO] ([email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261111200524_InitialAzureSql', N'10.0.0');

COMMIT;
GO

