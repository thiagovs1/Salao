CREATE DATABASE IF NOT EXISTS Belleza
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE Belleza;

-- =========================================
-- CLIENTES
-- =========================================

CREATE TABLE IF NOT EXISTS Clientes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Telefone VARCHAR(20) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    UNIQUE KEY UQ_Clientes_Email (Email),
    UNIQUE KEY UQ_Clientes_Telefone (Telefone)
);

-- =========================================
-- SERVIÇOS
-- =========================================

CREATE TABLE IF NOT EXISTS Servicos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Preco DECIMAL(10,2) NOT NULL,
    DuracaoMinutos INT NOT NULL
);

-- =========================================
-- PROFISSIONAIS
-- =========================================

CREATE TABLE IF NOT EXISTS Profissionais (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Profissao VARCHAR(100) NOT NULL,
    Foto VARCHAR(255) NOT NULL DEFAULT '',
    Ativo BOOLEAN NOT NULL DEFAULT TRUE,
    Imagem VARCHAR(255) NOT NULL DEFAULT ''
);

-- =========================================
-- HORÁRIOS DE TRABALHO
-- DiaSemana: 1 = segunda ... 5 = sexta
-- =========================================

CREATE TABLE IF NOT EXISTS HorariosProfissionais (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ProfissionalId INT NOT NULL,
    DiaSemana INT NOT NULL,
    HoraInicio TIME NOT NULL,
    HoraFim TIME NOT NULL,

    CONSTRAINT CK_Horarios_DiaSemana
        CHECK (DiaSemana BETWEEN 1 AND 7),

    CONSTRAINT CK_Horarios_Intervalo
        CHECK (HoraInicio < HoraFim),

    CONSTRAINT UQ_Horarios_Profissional_Dia_Hora
        UNIQUE (
            ProfissionalId,
            DiaSemana,
            HoraInicio,
            HoraFim
        ),

    CONSTRAINT FK_HorariosProfissionais_Profissionais
        FOREIGN KEY (ProfissionalId)
        REFERENCES Profissionais(Id)
        ON DELETE CASCADE
);

-- =========================================
-- COMBOS
-- =========================================

CREATE TABLE IF NOT EXISTS `Combo` (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Preco DECIMAL(10,2) NOT NULL
);

-- =========================================
-- SERVIÇOS DOS COMBOS
-- =========================================

CREATE TABLE IF NOT EXISTS ComboServico (
    ComboId INT NOT NULL,
    ServicoId INT NOT NULL,

    PRIMARY KEY (ComboId, ServicoId),

    CONSTRAINT FK_ComboServico_Combo
        FOREIGN KEY (ComboId)
        REFERENCES `Combo`(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_ComboServico_Servicos
        FOREIGN KEY (ServicoId)
        REFERENCES Servicos(Id)
        ON DELETE CASCADE
);

-- =========================================
-- USUÁRIOS
-- =========================================

CREATE TABLE IF NOT EXISTS Usuario (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    Senha VARCHAR(255) NOT NULL,
    TipoUsuario VARCHAR(30) NOT NULL DEFAULT 'Cliente',
    ProfissionalId INT NULL,

    UNIQUE KEY UQ_Usuario_Email (Email),

    CONSTRAINT FK_Usuario_Profissionais
        FOREIGN KEY (ProfissionalId)
        REFERENCES Profissionais(Id)
        ON DELETE SET NULL
);

-- =========================================
-- AGENDAMENTOS
-- =========================================

CREATE TABLE IF NOT EXISTS Agendamentos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    DataHora DATETIME NOT NULL,
    ClienteId INT NOT NULL,
    ServicoId INT NULL,
    ProfissionalId INT NOT NULL,
    Status VARCHAR(30) NOT NULL DEFAULT 'Aberto',
    ComboId INT NULL,

    CONSTRAINT FK_Agendamentos_Clientes
        FOREIGN KEY (ClienteId)
        REFERENCES Clientes(Id),

    CONSTRAINT FK_Agendamentos_Servicos
        FOREIGN KEY (ServicoId)
        REFERENCES Servicos(Id)
        ON DELETE SET NULL,

    CONSTRAINT FK_Agendamentos_Profissionais
        FOREIGN KEY (ProfissionalId)
        REFERENCES Profissionais(Id),

    CONSTRAINT FK_Agendamentos_Combo
        FOREIGN KEY (ComboId)
        REFERENCES `Combo`(Id)
        ON DELETE SET NULL
);

ALTER TABLE Agendamentos
ADD COLUMN FormaPagamento VARCHAR(30) NULL;

-- =========================================
-- DADOS DOS SERVIÇOS
-- =========================================

INSERT INTO Servicos
    (Id, Nome, Preco, DuracaoMinutos)
VALUES
    (1, 'Corte', 40.00, 60),
    (2, 'Hidratação', 50.00, 60),
    (3, 'Manicure', 30.00, 45),
    (4, 'Pedicure', 35.00, 45),
    (5, 'Penteado', 70.00, 90),
    (6, 'Maquiagem', 90.00, 90),
    (7, 'Design de Sobrancelha', 25.00, 30)
ON DUPLICATE KEY UPDATE
    Nome = VALUES(Nome),
    Preco = VALUES(Preco),
    DuracaoMinutos = VALUES(DuracaoMinutos);

-- =========================================
-- PROFISSIONAIS
-- =========================================

INSERT INTO Profissionais
    (Id, Nome, Profissao, Foto, Ativo, Imagem)
VALUES
    (1, 'Camila Ferreira', 'Cabeleireira',
     'camila.png', TRUE, 'camila.png'),
    (2, 'Beatriz Costa', 'Cabeleireira',
     'beatriz.png', TRUE, 'beatriz.png')
ON DUPLICATE KEY UPDATE
    Nome = VALUES(Nome),
    Profissao = VALUES(Profissao),
    Foto = VALUES(Foto),
    Ativo = VALUES(Ativo),
    Imagem = VALUES(Imagem);

-- =========================================
-- COMBOS
-- =========================================

INSERT INTO `Combo` (Id, Nome, Preco)
VALUES
    (1, 'Dia de beleza', 130.00),
    (2, 'Produção para festa', 160.00),
    (3, 'Belleza completa', 190.00)
ON DUPLICATE KEY UPDATE
    Nome = VALUES(Nome),
    Preco = VALUES(Preco);

-- =========================================
-- SERVIÇOS DOS COMBOS
-- =========================================

INSERT INTO ComboServico (ComboId, ServicoId)
VALUES
    (1, 1), (1, 2), (1, 3), (1, 4),
    (2, 5), (2, 6), (2, 7),
    (3, 1), (3, 2), (3, 3), (3, 4), (3, 6), (3, 7)
ON DUPLICATE KEY UPDATE
    ServicoId = VALUES(ServicoId);

-- =========================================
-- HORÁRIOS DAS PROFISSIONAIS
-- Segunda a sexta:
-- 08:00 às 12:00 e 13:00 às 18:00
-- =========================================

INSERT INTO HorariosProfissionais
    (ProfissionalId, DiaSemana, HoraInicio, HoraFim)
SELECT
    p.Id,
    d.DiaSemana,
    d.HoraInicio,
    d.HoraFim
FROM Profissionais p
CROSS JOIN (
    SELECT 1 AS DiaSemana, '08:00:00' AS HoraInicio, '12:00:00' AS HoraFim
    UNION ALL SELECT 1, '13:00:00', '18:00:00'
    UNION ALL SELECT 2, '08:00:00', '12:00:00'
    UNION ALL SELECT 2, '13:00:00', '18:00:00'
    UNION ALL SELECT 3, '08:00:00', '12:00:00'
    UNION ALL SELECT 3, '13:00:00', '18:00:00'
    UNION ALL SELECT 4, '08:00:00', '12:00:00'
    UNION ALL SELECT 4, '13:00:00', '18:00:00'
    UNION ALL SELECT 5, '08:00:00', '12:00:00'
    UNION ALL SELECT 5, '13:00:00', '18:00:00'
) d
WHERE NOT EXISTS (
    SELECT 1
    FROM HorariosProfissionais h
    WHERE h.ProfissionalId = p.Id
      AND h.DiaSemana = d.DiaSemana
      AND h.HoraInicio = d.HoraInicio
      AND h.HoraFim = d.HoraFim
);

-- =========================================
-- CONTAS DAS FUNCIONÁRIAS
-- =========================================

INSERT INTO Usuario
    (Nome, Email, Senha, TipoUsuario, ProfissionalId)
VALUES
    ('Camila Ferreira', 'camila@belleza.com',
     '123456', 'Funcionario', 1),
    ('Beatriz Costa', 'beatriz@belleza.com',
     '123456', 'Funcionario', 2)
ON DUPLICATE KEY UPDATE
    Nome = VALUES(Nome),
    Senha = VALUES(Senha),
    TipoUsuario = VALUES(TipoUsuario),
    ProfissionalId = VALUES(ProfissionalId);

-- =========================================
-- CONFERÊNCIA DOS VÍNCULOS
-- =========================================

SELECT
    u.Id,
    u.Nome,
    u.Email,
    u.TipoUsuario,
    u.ProfissionalId,
    p.Nome AS ProfissionalVinculada
FROM Usuario u
LEFT JOIN Profissionais p
    ON p.Id = u.ProfissionalId
ORDER BY u.Id;
