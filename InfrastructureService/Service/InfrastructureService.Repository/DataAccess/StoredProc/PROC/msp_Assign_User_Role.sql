USE InfrastructureService
GO

CREATE PROC msp_Assign_User_Role
	@UserID UNIQUEIDENTIFIER,
	@RoleIDList VARCHAR(MAX)
AS
BEGIN
	BEGIN TRAN
	
	DELETE FROM Auth_User_Role WHERE UserID = @UserID
	
	DECLARE @IDTable TABLE (item UNIQUEIDENTIFIER)
	INSERT @IDTable
	SELECT CAST(fs.item AS UNIQUEIDENTIFIER) FROM dbo.func_Split(@RoleIDList,',') fs
	
	DECLARE @RoleID UNIQUEIDENTIFIER
	DECLARE reader CURSOR 
	FOR SELECT * FROM @IDTable
	OPEN reader
	
	FETCH NEXT FROM reader INTO @RoleID
	WHILE @@FETCH_STATUS = 0
	BEGIN
		INSERT INTO Auth_User_Role
		(
			UserID,
			RoleID
		)
		VALUES
		(
			@UserID,
			@RoleID
		)
		
		FETCH NEXT FROM reader INTO @RoleID
	END
	
	CLOSE reader	
	DEALLOCATE reader
	
	IF @@ERROR <> 0
	BEGIN
		ROLLBACK
	END
	ELSE
	BEGIN
		COMMIT
	END
END
GO

--GRANT EXEC ON [dbo].[msp_Assign_User_Role] TO Manager