USE [InfrastructureService]
GO

CREATE FUNCTION [dbo].[func_Split]
(
	@String   VARCHAR(MAX),
	@Pattern  VARCHAR(8) = ','
)
RETURNS @temp TABLE(item VARCHAR(50))
AS
BEGIN
	SET @String = RTRIM(LTRIM(@String))
	
	DECLARE @index INT
	SET @index = CHARINDEX(@Pattern, @String)
	WHILE @index >= 1
	BEGIN
	    INSERT @temp VALUES (LEFT(@String, @index - 1))
	    SET @String = RIGHT(@String, LEN(@String) - @index)
	    SET @index = CHARINDEX(@Pattern, @String)
	END
	
	IF @String <> ''
	    INSERT @temp VALUES (@String)
	
	RETURN
END
GO