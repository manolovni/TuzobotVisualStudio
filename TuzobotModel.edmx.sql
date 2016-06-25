
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 06/25/2016 16:44:21
-- Generated from EDMX file: C:\Users\Nikolay\Documents\Visual Studio 2015\Projects\TuzobotGit\Tuzobot\TuzobotModel.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [tuzodb];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------


-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'ConvSet'
CREATE TABLE [dbo].[ConvSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [ConversationId] nvarchar(max)  NOT NULL,
    [UserAddress] nvarchar(max)  NOT NULL,
    [BotAddress] nvarchar(max)  NOT NULL,
    [ChannelId] nvarchar(max)  NOT NULL,
    [LastActive] datetime  NOT NULL,
    [Deleted] bit  NOT NULL
);
GO

-- Creating table 'ContestSet'
CREATE TABLE [dbo].[ContestSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Description] nvarchar(max)  NOT NULL,
    [Image] nvarchar(max)  NOT NULL,
    [EndDate] datetime  NOT NULL,
    [Active] bit  NOT NULL,
    [NumberOfWinners] int  NOT NULL,
    [Type] int  NOT NULL
);
GO

-- Creating table 'SubmitSet'
CREATE TABLE [dbo].[SubmitSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Image] nvarchar(max)  NOT NULL,
    [ConvId] int  NOT NULL,
    [ContestId] int  NOT NULL,
    [Score] float  NULL,
    [IsNotAdult] bit  NOT NULL,
    [IsWinner] bit  NOT NULL,
    [Promoceode] nvarchar(max)  NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'ConvSet'
ALTER TABLE [dbo].[ConvSet]
ADD CONSTRAINT [PK_ConvSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'ContestSet'
ALTER TABLE [dbo].[ContestSet]
ADD CONSTRAINT [PK_ContestSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'SubmitSet'
ALTER TABLE [dbo].[SubmitSet]
ADD CONSTRAINT [PK_SubmitSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [ConvId] in table 'SubmitSet'
ALTER TABLE [dbo].[SubmitSet]
ADD CONSTRAINT [FK_ConvSubmit]
    FOREIGN KEY ([ConvId])
    REFERENCES [dbo].[ConvSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_ConvSubmit'
CREATE INDEX [IX_FK_ConvSubmit]
ON [dbo].[SubmitSet]
    ([ConvId]);
GO

-- Creating foreign key on [ContestId] in table 'SubmitSet'
ALTER TABLE [dbo].[SubmitSet]
ADD CONSTRAINT [FK_SubmitContest]
    FOREIGN KEY ([ContestId])
    REFERENCES [dbo].[ContestSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_SubmitContest'
CREATE INDEX [IX_FK_SubmitContest]
ON [dbo].[SubmitSet]
    ([ContestId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------