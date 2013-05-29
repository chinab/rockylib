USE InfrastructureService
GO

CREATE PROC msp_BlockRole
	@RoleID UNIQUEIDENTIFIER
AS
BEGIN
	IF EXISTS(SELECT 1 FROM Auth_User_Role aur WHERE aur.RoleID = @RoleID)
	BEGIN
		RETURN -1
	END
	
	BEGIN TRAN
	
	DELETE FROM RoleInfo WHERE RowID = @RoleID
	
	DELETE FROM Auth_Role_Control WHERE RoleID = @RoleID
	
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

--GRANT EXEC ON [dbo].[msp_BlockRole] TO Manager