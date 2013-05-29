USE InfrastructureService
GO

CREATE PROC msp_GetUserControl
	@UserID UNIQUEIDENTIFIER,
	@ComponentID UNIQUEIDENTIFIER
AS
BEGIN
	SELECT DISTINCT ci.RowID,ci.[Name],ci.Description,ci.[Path],ci.Sort FROM
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
	INNER JOIN ComponentInfo ci2 ON ci2.RowID = ci.ComponentID
	WHERE ci.Status >= 0
	AND ci.ComponentID = @ComponentID
	ORDER BY ci.Sort
END
go

--GRANT EXEC ON [dbo].[msp_GetUserControl] TO Manager