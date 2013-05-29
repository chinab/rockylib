USE [InfrastructureService]
GO

CREATE PROC csp_RhnzAutoComplete
	@ComponentID UNIQUEIDENTIFIER,
	@Keyword VARCHAR(25),
	@SegmentWords VARCHAR(MAX)
AS
BEGIN
	DECLARE @RecordCount INT
	SELECT @RecordCount = COUNT(1) FROM [EC_Enterprise].dbo.Commodity(NOLOCK) mc
	WHERE 1 <= ANY(SELECT CHARINDEX(fs.item,mc.[Name]) FROM dbo.func_Split(@SegmentWords,' ') fs)
	
	IF @RecordCount = 0
	BEGIN
		SELECT TOP 10 t.Keyword,t.RecordCount FROM SearchKeyword t
		WHERE t.ComponentID = @ComponentID
		AND 
		(
			t.Pinyin LIKE @Keyword+'%'
			OR t.PinyinCaps LIKE @Keyword+'%'
		)
		ORDER BY t.[Count] DESC
	END
	ELSE
	BEGIN
		IF EXISTS(SELECT 1 FROM SearchKeyword t WHERE t.ComponentID = @ComponentID AND t.Keyword = @Keyword)
		BEGIN
			UPDATE SearchKeyword
			SET
				[Count] = [COUNT] + 1,
				RecordCount = @RecordCount
			WHERE ComponentID = @ComponentID AND Keyword = @Keyword
		END
		ELSE
		BEGIN
			INSERT INTO SearchKeyword
			(
				ComponentID,
				Keyword,
				[Count],
				RecordCount
			)
			VALUES
			(
				@ComponentID,
				@Keyword,
				1,
				@RecordCount
			)
		END
		
		SELECT TOP 10 t.Keyword,t.RecordCount FROM SearchKeyword t
		WHERE t.ComponentID = @ComponentID
		AND t.Keyword LIKE @Keyword+'%'
		ORDER BY t.[Count] DESC
	END
END
GO

--GRANT EXEC ON [dbo].[csp_AutoComplete] TO Manager