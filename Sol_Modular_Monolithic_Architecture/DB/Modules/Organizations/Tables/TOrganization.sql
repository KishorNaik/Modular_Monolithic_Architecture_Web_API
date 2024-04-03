CREATE TABLE [OrgSchema].[TOrganization]
(
	[Id] NUMERIC identity(1,1) NOT NULL PRIMARY KEY, 
    [Identifier] UNIQUEIDENTIFIER NOT NULL, 
    [Name] VARCHAR(50) NOT NULL, 
    [OrgType] INT NOT NULL,
    [Status] BIT NOT NULL, 
    [CreatedDate] DATETIME NULL, 
    [ModifiedDate] DATETIME NULL, 
    [Version] ROWVERSION NULL,
)
