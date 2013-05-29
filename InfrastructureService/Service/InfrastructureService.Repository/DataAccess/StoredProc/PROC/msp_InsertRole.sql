USE InfrastructureService
GO

CREATE PROC msp_InsertRole
	@Name VARCHAR(50),
	@Description VARCHAR(255),
	@ControlIDList VARCHAR(MAX),
	@PermissionFlagsList VARCHAR(MAX)
AS
BEGIN
	DECLARE @RoleID UNIQUEIDENTIFIER
	SELECT @RoleID = ri.RowID FROM RoleInfo ri WHERE ri.[Name] = @Name
	
	BEGIN TRAN
	
	IF @RoleID IS NULL
	BEGIN
		SET @RoleID = NEWID()
		INSERT INTO RoleInfo
		(
			RowID,
			[Name],
			[Description]
		)
		VALUES
		(
			@RoleID,
			@Name,
			@Description
		)
	END
	ELSE
	BEGIN
		UPDATE RoleInfo
		SET
			[Description] = @Description
		WHERE [Name] = @Name
	END
	
	IF @@ERROR <> 0
	BEGIN
		ROLLBACK
		RETURN
	END
	
	EXEC msp_Assign_Role_Control @RoleID,@ControlIDList,@PermissionFlagsList
	
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

--GRANT EXEC ON [dbo].[msp_InsertRole] TO Manager