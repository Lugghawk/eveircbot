USE [Eve]
GO

/****** Object: Table [dbo].[invTypes] Script Date: 8/19/2013 11:20:15 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[invTypes] (
    [typeID]              INT             NOT NULL,
    [groupID]             INT             NULL,
    [typeName]            NVARCHAR (100)  COLLATE Latin1_General_CI_AI NULL,
    [description]         NVARCHAR (3000) NULL,
    [mass]                FLOAT (53)      NULL,
    [volume]              FLOAT (53)      NULL,
    [capacity]            FLOAT (53)      NULL,
    [portionSize]         INT             NULL,
    [raceID]              TINYINT         NULL,
    [basePrice]           MONEY           NULL,
    [published]           BIT             NULL,
    [marketGroupID]       INT             NULL,
    [chanceOfDuplicating] FLOAT (53)      NULL
);


