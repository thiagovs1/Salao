          AND TABLE_NAME = 'Profissionais'
          AND COLUMN_NAME = 'Endereco'
    ) THEN
        ALTER TABLE Profissionais
        ADD COLUMN Endereco VARCHAR(255) NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Profissionais'
          AND COLUMN_NAME = 'TaxaAtendimento'
    ) THEN
        ALTER TABLE Profissionais
        ADD COLUMN TaxaAtendimento DECIMAL(10,2) NOT NULL DEFAULT 0.00;
    END IF;
END$$

DELIMITER ;

CALL AtualizarProfissionais();

DROP PROCEDURE IF EXISTS AtualizarProfissionais;

-- Conferir as colunas
DESCRIBE Profissionais;
DROP PROCEDURE IF EXISTS AtualizarEstruturaBelleza;

DELIMITER $$

CREATE PROCEDURE AtualizarEstruturaBelleza()
BEGIN

    -- Dados adicionais dos profissionais
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Profissionais'
          AND COLUMN_NAME = 'CPF'
    ) THEN
        ALTER TABLE Profissionais
        ADD COLUMN CPF VARCHAR(14) NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Profissionais'
          AND COLUMN_NAME = 'Telefone'
    ) THEN
        ALTER TABLE Profissionais
        ADD COLUMN Telefone VARCHAR(20) NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Profissionais'
          AND COLUMN_NAME = 'Endereco'
    ) THEN
        ALTER TABLE Profissionais
        ADD COLUMN Endereco VARCHAR(255) NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Profissionais'
          AND COLUMN_NAME = 'TaxaAtendimento'
    ) THEN
        ALTER TABLE Profissionais
        ADD COLUMN TaxaAtendimento DECIMAL(10,2) NOT NULL DEFAULT 0.00;
    END IF;

    -- Forma de pagamento dos agendamentos
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Agendamentos'
          AND COLUMN_NAME = 'FormaPagamento'
    ) THEN
        ALTER TABLE Agendamentos
        ADD COLUMN FormaPagamento VARCHAR(30) NULL;
    END IF;

    -- Imagem dos combos
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Combo'
          AND COLUMN_NAME = 'Imagem'
    ) THEN
        ALTER TABLE `Combo`
        ADD COLUMN Imagem VARCHAR(255) NULL;
    END IF;

END$$

DELIMITER ;

CALL AtualizarEstruturaBelleza();

DROP PROCEDURE IF EXISTS AtualizarEstruturaBelleza;

-- Conferir as colunas dos profissionais
SHOW COLUMNS FROM Profissionais;

-- Conferir as colunas dos agendamentos
SHOW COLUMNS FROM Agendamentos;

-- Conferir as colunas dos combos
SHOW COLUMNS FROM `Combo`;
CREATE TABLE IF NOT EXISTS Despesas (
    Id INT NOT NULL AUTO_INCREMENT,
    Descricao VARCHAR(200) NOT NULL,
    Categoria VARCHAR(100) NOT NULL DEFAULT 'Outros',
    Valor DECIMAL(10,2) NOT NULL,
    Data DATETIME NOT NULL,
    PRIMARY KEY (Id)
);
