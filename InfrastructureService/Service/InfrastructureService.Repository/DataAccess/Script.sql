USE ClusterService
GO

--1.´´½¨App
INSERT INTO AppInfo
(
	AppID,
	AppName,
	CreateDate,
	[Status]
)
VALUES
(
	NEWID(),
	'ECCrawler',
	GETDATE(),
	0
)

SELECT * FROM AppInfo ai
--2.
INSERT INTO EmailConfig
(
	RowID,
	SmtpAuthority,
	UserName,
	[Password],
	FromEmail,
	FromDisplayName,
	EnableSsl,
	CreateDate
)
VALUES
(
	NEWID(),
	'smtp.163.com',
	'sys_rocky@163.com',
	'4everyoung',
	'sys_rocky@163.com',
	'ECCrawler Support',
	0,
	GETDATE()
)

SELECT * FROM EmailConfig ec