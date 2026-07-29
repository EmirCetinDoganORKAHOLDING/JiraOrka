using Dapper;
using Tezgah.Data;
using Tezgah.Models;
using TaskStatus = Tezgah.Models.TaskStatus;

namespace Tezgah.Repositories;

public class TaskRepository
{
    private readonly DapperContext _context;

    public TaskRepository(DapperContext context) => _context = context;

    private const string BaseSelect = @"
        SELECT t.*,
               p.Name     AS ProjectName,
               p.GroupId,
               g.Name     AS GroupName,
               a.FullName AS AssignedToName,
               a.Email    AS AssignedToEmail,
               c.FullName AS CreatedByName,
               COALESCE(r.FullName, t.RequesterName) AS RequesterName,
               r.Email    AS RequesterEmail
        FROM OrkaJira_Tasks t
        LEFT JOIN OrkaJira_Projects        p ON t.ProjectId   = p.Id
        LEFT JOIN OrkaJira_PermissionGroups g ON p.GroupId     = g.Id
        LEFT JOIN OrkaJira_Users           a ON t.AssignedToId = a.Id
        LEFT JOIN OrkaJira_Users           c ON t.CreatedById  = c.Id
        LEFT JOIN OrkaJira_Users           r ON t.RequesterId  = r.Id";

    public async Task<IEnumerable<TaskItem>> GetByProjectAsync(int projectId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<TaskItem>(
            BaseSelect + " WHERE t.ProjectId = @ProjectId ORDER BY t.CreatedAt DESC",
            new { ProjectId = projectId });
    }

    public async Task<IEnumerable<TaskItem>> GetByGroupAsync(int groupId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<TaskItem>(
            BaseSelect + " WHERE p.GroupId = @GroupId ORDER BY t.CreatedAt DESC",
            new { GroupId = groupId });
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<TaskItem>(BaseSelect + " ORDER BY t.CreatedAt DESC");
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var task = await conn.QueryFirstOrDefaultAsync<TaskItem>(
            BaseSelect + " WHERE t.Id = @Id", new { Id = id });

        if (task != null)
        {
            var comments = (await conn.QueryAsync<Comment>(@"
                SELECT cm.*, u.FullName AS UserFullName
                FROM OrkaJira_Comments cm
                LEFT JOIN OrkaJira_Users u ON cm.UserId = u.Id
                WHERE cm.TaskId = @TaskId
                ORDER BY cm.CreatedAt ASC", new { TaskId = id })).ToList();

            if (comments.Count > 0)
            {
                var commentIds = comments.Select(c => c.Id).ToList();
                var attachments = await conn.QueryAsync<CommentAttachment>(@"
                    SELECT * FROM OrkaJira_CommentAttachments
                    WHERE CommentId IN @Ids
                    ORDER BY UploadedAt ASC", new { Ids = commentIds });

                var attachMap = attachments.GroupBy(a => a.CommentId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var c in comments)
                    c.Attachments = attachMap.TryGetValue(c.Id, out var list) ? list : new();
            }

            task.Comments = comments;
        }

        return task;
    }

    public async Task<int> CreateAsync(TaskItem task)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO OrkaJira_Tasks
                (Title, Description, ProjectId, AssignedToId, CreatedById,
                 RequesterId, RequesterName, RequesterDetails, Status, Priority, DueDate)
            VALUES
                (@Title, @Description, @ProjectId, @AssignedToId, @CreatedById,
                 @RequesterId, @RequesterName, @RequesterDetails, @Status, @Priority, @DueDate);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                task.Title, task.Description, task.ProjectId,
                task.AssignedToId, task.CreatedById,
                task.RequesterId, task.RequesterName, task.RequesterDetails,
                Status   = (int)task.Status,
                Priority = (int)task.Priority,
                task.DueDate
            });
    }

    public async Task UpdateAsync(TaskItem task)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE OrkaJira_Tasks SET
                Title            = @Title,
                Description      = @Description,
                ProjectId        = @ProjectId,
                AssignedToId     = @AssignedToId,
                RequesterId      = @RequesterId,
                RequesterName    = @RequesterName,
                RequesterDetails = @RequesterDetails,
                Status           = @Status,
                Priority         = @Priority,
                DueDate          = @DueDate,
                UpdatedAt        = GETUTCDATE()
            WHERE Id = @Id",
            new
            {
                task.Title, task.Description, task.ProjectId,
                task.AssignedToId, task.RequesterId,
                task.RequesterName, task.RequesterDetails,
                Status   = (int)task.Status,
                Priority = (int)task.Priority,
                task.DueDate,
                task.Id
            });
    }

    public async Task UpdateStatusAsync(int taskId, TaskStatus status)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE OrkaJira_Tasks
            SET Status = @Status, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id",
            new { Status = (int)status, Id = taskId });
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM OrkaJira_Tasks WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> AddCommentAsync(Comment comment)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO OrkaJira_Comments (TaskId, UserId, Content)
            VALUES (@TaskId, @UserId, @Content);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", comment);
    }

    public async Task AddAttachmentAsync(CommentAttachment attachment)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO OrkaJira_CommentAttachments
                (CommentId, FileName, StoredFileName, ContentType, FileSize)
            VALUES
                (@CommentId, @FileName, @StoredFileName, @ContentType, @FileSize)",
            attachment);
    }

    public async Task<IEnumerable<TaskItem>> GetAssignedToUserAsync(int userId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<TaskItem>(
            BaseSelect + @"
            WHERE t.AssignedToId = @UserId AND t.Status <> 3
            ORDER BY t.Priority DESC, t.DueDate ASC",
            new { UserId = userId });
    }

    public async Task<IEnumerable<TaskItem>> GetAllAssignedToUserAsync(int userId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<TaskItem>(
            BaseSelect + " WHERE t.AssignedToId = @UserId ORDER BY t.CreatedAt DESC",
            new { UserId = userId });
    }

    public async Task<IEnumerable<TaskItem>> GetCreatedByUserAsync(int userId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<TaskItem>(
            BaseSelect + " WHERE t.CreatedById = @UserId ORDER BY t.CreatedAt DESC",
            new { UserId = userId });
    }

    public async Task<IEnumerable<TaskItem>> GetFilteredAsync(
        DateTime? startDate    = null,
        DateTime? endDate      = null,
        string?   requesterName = null,
        int?      assignedToId = null,
        int?      status       = null,
        int?      priority     = null,
        int?      projectId    = null,
        int?      groupId      = null)
    {
        var where = new List<string>();
        if (startDate.HasValue)                    where.Add("t.CreatedAt    >= @StartDate");
        if (endDate.HasValue)                      where.Add("t.CreatedAt    <  @EndDatePlusOne");
        if (!string.IsNullOrEmpty(requesterName))  where.Add("(t.RequesterName LIKE @RequesterName OR r.FullName LIKE @RequesterName)");
        if (assignedToId.HasValue)                 where.Add("t.AssignedToId =  @AssignedToId");
        if (status.HasValue)                       where.Add("t.Status       =  @Status");
        if (priority.HasValue)                     where.Add("t.Priority     =  @Priority");
        if (projectId.HasValue)                    where.Add("t.ProjectId    =  @ProjectId");
        if (groupId.HasValue)                      where.Add("p.GroupId      =  @GroupId");

        var sql = BaseSelect
            + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")
            + " ORDER BY t.CreatedAt DESC";

        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<TaskItem>(sql, new
        {
            StartDate      = startDate,
            EndDatePlusOne = endDate.HasValue ? endDate.Value.AddDays(1) : (DateTime?)null,
            RequesterName  = !string.IsNullOrEmpty(requesterName) ? $"%{requesterName}%" : null,
            AssignedToId   = assignedToId,
            Status         = status,
            Priority       = priority,
            ProjectId      = projectId,
            GroupId        = groupId
        });
    }
}
