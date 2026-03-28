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
CREATE TABLE [Clientes] (
    [Id] uniqueidentifier NOT NULL,
    [RazonSocial] nvarchar(max) NOT NULL,
    [Nit] nvarchar(max) NOT NULL,
    [Departamento] nvarchar(max) NOT NULL,
    [Ciudad] nvarchar(max) NOT NULL,
    [Direccion] nvarchar(max) NOT NULL,
    [Telefono] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [ContactoPrincipal] nvarchar(max) NOT NULL,
    [Estado] nvarchar(max) NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    [UsuarioCreacion] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
);

CREATE TABLE [Rol] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(50) NOT NULL,
    [Descripcion] nvarchar(max) NULL,
    CONSTRAINT [PK_Rol] PRIMARY KEY ([Id])
);

CREATE TABLE [Usuario] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [Nombre] nvarchar(100) NOT NULL,
    [AvatarUrl] nvarchar(max) NULL,
    [Estado] nvarchar(max) NOT NULL,
    [FechaRegistro] datetime2 NOT NULL,
    [RolId] int NOT NULL,
    CONSTRAINT [PK_Usuario] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Usuario_Rol_RolId] FOREIGN KEY ([RolId]) REFERENCES [Rol] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Descripcion', N'Nombre') AND [object_id] = OBJECT_ID(N'[Rol]'))
    SET IDENTITY_INSERT [Rol] ON;
INSERT INTO [Rol] ([Id], [Descripcion], [Nombre])
VALUES (1, N'Acceso total a RmsApp', N'Admin'),
(2, N'Puede crear y editar, pero no eliminar', N'Operador'),
(3, N'Solo lectura de información', N'Consulta');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Descripcion', N'Nombre') AND [object_id] = OBJECT_ID(N'[Rol]'))
    SET IDENTITY_INSERT [Rol] OFF;

CREATE UNIQUE INDEX [IX_Usuario_Email] ON [Usuario] ([Email]);

CREATE INDEX [IX_Usuario_RolId] ON [Usuario] ([RolId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260321021019_InitialCreate', N'9.0.2');

COMMIT;
GO

