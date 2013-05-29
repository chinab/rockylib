USE [ClusterService]
GO

ALTER PROC csp_Xfjob_JobAutoComplete
	@ComponentID UNIQUEIDENTIFIER,
	@Keyword VARCHAR(25),
	@SegmentWords VARCHAR(MAX)
AS
BEGIN
	DECLARE @RecordCount INT
	SELECT @RecordCount = COUNT(1) FROM [UUNet.Xfjob].dbo.Job_Base(NOLOCK) jb
	INNER JOIN [UUNet.Xfjob].dbo.Company_Base(NOLOCK) cb ON jb.Comid = cb.Comid
	WHERE 1 <= ANY(SELECT CHARINDEX(fs.item,jb.JobName) FROM dbo.func_Split(@SegmentWords,' ') fs)
	OR cb.CompanyName = @SegmentWords
	
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