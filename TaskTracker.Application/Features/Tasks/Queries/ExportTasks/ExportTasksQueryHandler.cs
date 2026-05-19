using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Tasks.Queries.ExportTasks;

public class ExportTasksQueryHandler : IRequestHandler<ExportTasksQuery, ExportTasksResult>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly ICurrentUserService _currentUser;

    public ExportTasksQueryHandler(
        ITaskRepository taskRepository,
        IMembershipRepository membershipRepository,
        ICurrentUserService currentUser)
    {
        _taskRepository = taskRepository;
        _membershipRepository = membershipRepository;
        _currentUser = currentUser;
    }

    public async Task<ExportTasksResult> Handle(ExportTasksQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.UserId!;

        // Verify membership unless super admin
        if (!_currentUser.IsSuperAdmin)
        {
            var projectIds = await _membershipRepository
                .GetUserProjectIdsAsync(currentUserId, cancellationToken);

            if (!projectIds.Contains(request.ProjectId))
                throw new ForbiddenAccessException(
                    $"No access to Project resource '{request.ProjectId}'.");
        }

        // Fetch project key for filename
        var projectKey = await _taskRepository.GetProjectKeyAsync(request.ProjectId, cancellationToken);
        if (string.IsNullOrWhiteSpace(projectKey))
            throw new KeyNotFoundException($"Project '{request.ProjectId}' was not found.");

        // Build query with navigation includes for human-readable names
        var query = _taskRepository.Query()
            .Where(t => t.ProjectId == request.ProjectId);

        if (request.BacklogOnly)
            query = query.Where(t => t.SprintId == null);

        var tasks = await query
            .Include(t => t.Epic)
            .Include(t => t.Sprint)
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Include(t => t.Reporter)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .ToListAsync(cancellationToken);

        var fileBytes = BuildWorkbook(tasks, request.BacklogOnly);
        return new ExportTasksResult(fileBytes, projectKey);
    }

    private static byte[] BuildWorkbook(List<TaskItem> tasks, bool backlogOnly)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Tasks");

        var headers = backlogOnly
            ? new[] { "Task Code", "Title", "Description", "Status", "Priority",
                    "Start Date", "End Date", "Epic Title", "Assignee", "Reporter" }
            : new[] { "Task Code", "Title", "Description", "Status", "Priority",
                    "Start Date", "End Date", "Epic Title", "Assignee", "Sprint Name",
                    "Is Expired", "Created At", "Updated At", "Project Name", "Reporter" };

        // Header row
        for (var col = 1; col <= headers.Length; col++)
        {
            var cell = sheet.Cell(1, col);
            cell.Value = headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        sheet.SheetView.FreezeRows(1);

        // Pre-compute counts — single pass
        var completedCount = 0;
        foreach (var t in tasks)
            if (t.Status == Status.Completed) completedCount++;
        var pendingCount = tasks.Count - completedCount;

        // Hoist branch decision outside loop
        Action<IXLRow, TaskItem> writeExtendedColumns = backlogOnly
            ? (r, t) => r.Cell(10).Value = FormatUserName(t.Reporter)
            : (r, t) =>
            {
                r.Cell(10).Value = t.Sprint?.Name ?? string.Empty;
                r.Cell(11).Value = t.IsExpired;
                SetDateCell(r.Cell(12), DateOnly.FromDateTime(t.CreatedAt));
                SetDateCell(r.Cell(13), DateOnly.FromDateTime(t.UpdatedAt));
                r.Cell(14).Value = t.Project?.Name ?? string.Empty;
                r.Cell(15).Value = FormatUserName(t.Reporter);
            };

        // Data rows
        for (var i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            var xlRow = sheet.Row(i + 2);

            xlRow.Cell(1).Value = task.TaskCode;
            xlRow.Cell(2).Value = task.Title;
            xlRow.Cell(3).Value = task.Description ?? string.Empty;
            xlRow.Cell(4).Value = task.Status.ToString();
            xlRow.Cell(5).Value = task.Priority.ToString();
            SetDateCell(xlRow.Cell(6), task.StartDate);
            SetDateCell(xlRow.Cell(7), task.EndDate);
            xlRow.Cell(8).Value = task.Epic?.Title ?? string.Empty;
            xlRow.Cell(9).Value = FormatUserName(task.Assignee);

            writeExtendedColumns(xlRow, task);
        }

        // Summary rows
        var summaryRow = tasks.Count + 4;
        void WriteSummary(string label, int value)
        {
            var labelCell = sheet.Cell(summaryRow, 1);
            labelCell.Value = label;
            labelCell.Style.Font.Bold = true;
            sheet.Cell(summaryRow, 2).Value = value;
            summaryRow++;
        }

        WriteSummary("Total Tasks:", tasks.Count);
        WriteSummary("Completed Tasks:", completedCount);
        WriteSummary("Pending Tasks:", pendingCount);

        sheet.Columns(1, headers.Length).AdjustToContents();

        using var stream = new MemoryStream(tasks.Count * 2048 + 8192);
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetDateCell(IXLCell cell, DateOnly? date)
    {
        if (!date.HasValue) return;
        cell.Value = date.Value.ToDateTime(TimeOnly.MinValue);
        cell.Style.DateFormat.Format = "yyyy-mm-dd";
    }

    private static string FormatUserName(ApplicationUser? user)
    {
        if (user is null)
            return string.Empty;

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return !string.IsNullOrWhiteSpace(fullName)
            ? fullName
            : user.Email ?? user.UserName ?? string.Empty;
    }
}
