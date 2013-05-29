USE InfrastructureService
GO

CREATE PROC msp_GetUserPermission
	@UserID UNIQUEIDENTIFIER,
	@Path VARCHAR(255),
	@PermissionFlags INT OUTPUT
AS
BEGIN
	SELECT @PermissionFlags = Max(t.PermissionFlags) FROM
	(
		SELECT arc.ControlID,arc.PermissionFlags
		FROM Auth_User_Role aur
		INNER JOIN Auth_Role_Control arc ON aur.RoleID = arc.RoleID
		WHERE aur.UserID = @UserID
		UNION
		SELECT auc.ControlID,auc.PermissionFlags FROM Auth_User_Control auc
		WHERE getdate() BETWEEN auc.BeginDate AND auc.EndDate
		AND auc.UserID = @UserID
	) t
	INNER JOIN ControlInfo ci ON t.ControlID = ci.RowID
	WHERE ci.Status >= 0
	AND ci.[Path] = @Path
END
GO

--GRANT EXEC ON [dbo].[msp_GetUserPermission] TO Manager

