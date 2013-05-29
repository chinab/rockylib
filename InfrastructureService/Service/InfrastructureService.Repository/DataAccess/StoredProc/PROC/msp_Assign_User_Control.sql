USE InfrastructureService
GO

CREATE PROC msp_Assign_User_Control
	@UserID UNIQUEIDENTIFIER,
	@ControlID UNIQUEIDENTIFIER,
	@PermissionFlags INT,
	@BeginDate SMALLDATETIME,
	@EndDate SMALLDATETIME
AS
BEGIN
	IF EXISTS(SELECT 1 FROM Auth_User_Control auc WHERE auc.UserID = @UserID AND auc.ControlID = @ControlID)
	BEGIN
		UPDATE Auth_User_Control
		SET
			PermissionFlags = @PermissionFlags,
			BeginDate = @BeginDate,
			EndDate = @EndDate
		WHERE UserID = @UserID 
		AND ControlID = @ControlID
	END
	ELSE
	BEGIN
		INSERT INTO Auth_User_Control
		(
			UserID,
			ControlID,
			PermissionFlags,
			BeginDate,
			EndDate
		)
		VALUES
		(
			@UserID,
			@ControlID,
			@PermissionFlags,
			@BeginDate,
			@EndDate
		)
	END
END
GO

--GRANT EXEC ON [dbo].[msp_Assign_User_Control] TO Manager