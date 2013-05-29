USE InfrastructureService
GO

CREATE PROC msp_Assign_Role_Control
	@RoleID UNIQUEIDENTIFIER,
	@ControlIDList VARCHAR(MAX),
	@PermissionFlagsList VARCHAR(MAX)
AS
BEGIN
	BEGIN TRAN
	
	DELETE FROM Auth_Role_Control WHERE RoleID = @RoleID
	
	DECLARE @ControlID UNIQUEIDENTIFIER
	DECLARE @PermissionFlags INT
	WHILE(charindex(',',@PermissionFlagsList) > 0)
	BEGIN
		SET @ControlID = substring(@ControlIDList,0,charindex(',',@ControlIDList))
		SET @PermissionFlags = substring(@PermissionFlagsList,0,charindex(',',@PermissionFlagsList))
		SET @ControlIDList = substring(@ControlIDList,charindex(',',@ControlIDList)+1,len(@ControlIDList))
		SET @PermissionFlagsList = substring(@PermissionFlagsList,charindex(',',@PermissionFlagsList)+1,len(@PermissionFlagsList))
		INSERT INTO Auth_Role_Control
		(
			RoleID,
			ControlID,
			PermissionFlags
		)
		VALUES
		(
			@RoleID,
			@ControlID,
			@PermissionFlags
		)
	END
	INSERT INTO Auth_Role_Control
	(
		RoleID,
		ControlID,
		PermissionFlags
	)
	VALUES
	(
		@RoleID,
		@ControlIDList,
		@PermissionFlagsList
	)
	
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

--GRANT EXEC ON [dbo].[msp_Assign_Role_Control] TO Manager