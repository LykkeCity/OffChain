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

USE [AssetLightning]
GO

/****** Object:  Table [dbo].[ChannelState]    Script Date: 9/28/2016 9:44:58 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ChannelState](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[StateName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_ChannelState] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

USE [AssetLightning]
GO

/****** Object:  Table [dbo].[Channel]    Script Date: 9/28/2016 9:43:33 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Channel](
	[Id] [uniqueidentifier] NOT NULL,
	[State] [bigint] NOT NULL,
	[Destination] [varchar](50) NOT NULL,
	[Asset] [varchar](50) NULL,
	[ContributedAmount] [float] NULL,
	[PeerContributedAmount] [float] NULL,
	[IsNegociationComplete] [bit] NULL,
 CONSTRAINT [PK_Channel] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[Channel]  WITH CHECK ADD  CONSTRAINT [FK_Channel_ChannelState] FOREIGN KEY([State])
REFERENCES [dbo].[ChannelState] ([Id])
GO

ALTER TABLE [dbo].[Channel] CHECK CONSTRAINT [FK_Channel_ChannelState]
GO

USE [AssetLightning]
GO

INSERT INTO [dbo].[ChannelState] ([StateName])  VALUES  ('Reset')
INSERT INTO [dbo].[ChannelState] ([StateName])  VALUES  ('HelloFinished')
INSERT INTO [dbo].[ChannelState] ([StateName])  VALUES  ('NegotiateChannelFinished')
INSERT INTO [dbo].[ChannelState] ([StateName])  VALUES  ('CreateBaseTransacionFinished')
GO



