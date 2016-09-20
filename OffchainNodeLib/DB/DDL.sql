CREATE DATABASE [AssetLightning]
GO

USE [AssetLightning]
GO

/****** Object:  Table [dbo].[PreGeneratedOutput]    Script Date: 9/16/2016 3:04:19 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[PreGeneratedOutput](
	[TransactionId] [varchar](100) NOT NULL,
	[OutputNumber] [int] NOT NULL,
	[Amount] [bigint] NOT NULL,
	[PrivateKey] [varchar](100) NOT NULL,
	[Consumed] [int] NOT NULL,
	[Script] [varchar](1000) NOT NULL,
	[AssetId] [varchar](100) NULL,
	[Address] [varchar](100) NULL,
	[Network] [varchar](10) NULL,
	[Version] [timestamp] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TransactionId] ASC,
	[OutputNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

USE [AssetLightning]
GO

/****** Object:  Table [dbo].[Session]    Script Date: 9/16/2016 3:04:31 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Session](
	[SessionId] [varchar](50) NOT NULL,
	[CreationDatetime] [datetime] NOT NULL,
	[PubKey] [varchar](100) NOT NULL,
	[Asset] [varchar](50) NULL,
	[RequestedAmount] [float] NULL,
	[Tolerance] [float] NULL,
	[ContributedAmount] [float] NULL,
	[Network] [varchar](10) NOT NULL,
 CONSTRAINT [PK_Session] PRIMARY KEY CLUSTERED 
(
	[SessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO