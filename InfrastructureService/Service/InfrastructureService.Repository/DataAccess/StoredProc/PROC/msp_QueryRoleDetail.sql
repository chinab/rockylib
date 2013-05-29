USE InfrastructureService
GO

CREATE PROC msp_QueryRoleDetail
	@RoleID UNIQUEIDENTIFIER
AS
BEGIN
	SELECT * FROM RoleInfo ri
	WHERE ri.RowID = @RoleID
	
--	DECLARE @hasRow BIT
--	IF @@ROWCOUNT > 0
--	BEGIN
--		SET @hasRow = 1
--	END
--	ELSE
--	BEGIN
--		SET @hasRow = 0
--	END
	
	
	DECLARE @IDTable TABLE (rowID UNIQUEIDENTIFIER)
	INSERT @IDTable
	SELECT ci.RowID FROM ServiceInfo si
	INNER JOIN ComponentInfo ci ON si.RowID = ci.ServiceID
	WHERE ci.[Status] >= 0
	ORDER BY si.Sort,ci.Sort
	
	SELECT si.[Name] ServiceName,ci.[Name] ComponentName
	FROM ServiceInfo si
	INNER JOIN ComponentInfo ci ON si.RowID = ci.ServiceID
	WHERE ci.[Status] >= 0
	ORDER BY si.Sort,ci.Sort
	
	DECLARE @ComponentID UNIQUEIDENTIFIER
	DECLARE reader CURSOR 
	FOR SELECT * FROM @IDTable
	OPEN reader
	
	FETCH NEXT FROM reader INTO @ComponentID
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SELECT ci.RowID,ci.[Name],ci.[Description],ISNULL(arc.PermissionFlags,0) PermissionFlags
		FROM ControlInfo ci
		LEFT JOIN Auth_Role_Control arc ON ci.RowID = arc.ControlID AND arc.RoleID = @RoleID
		WHERE ci.[Status] >= 0
		AND ci.ComponentID = @ComponentID
		ORDER BY ci.Sort
		
		FETCH NEXT FROM reader INTO @ComponentID
	END
	
	CLOSE reader	
	DEALLOCATE reader
END
GO

--GRANT EXEC ON [dbo].[msp_QueryRoleDetail] TO Manager