using System.Globalization;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Tasks.Commands.ImportTasks;

public class ImportTasksCommandHandler : IRequestHandler<ImportTasksCommand, TaskImportResultDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IEpicRepository _epicRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly ICurrentUserService _currentUser;
    private static readonly string[] ExpectedHeaders =
    [
        "Task Code", "Title", "Description", "Status", "Priority",
        "Start Date", "End Date", "Epic Title", "Assignee",
    ];

    private const int RequiredHeaderCount = 9;

    public ImportTasksCommandHandler(
        ITaskRepository taskRepository,
        IEpicRepository epicRepository,
        ISprintRepository sprintRepository,
        IMembershipRepository membershipRepository,
        ICurrentUserService currentUser)
    {
        _taskRepository = taskRepository;
        _epicRepository = epicRepository;
        _sprintRepository = sprintRepository;
        _membershipRepository = membershipRepository;
        _currentUser = currentUser;
    }

    public async Task<TaskImportResultDto> Handle(
        ImportTasksCommand command,
        CancellationToken cancellationToken)
    {
        var result = new TaskImportResultDto();
        var currentUserId = _currentUser.UserId!;

        // Verify project exists
        var projectKey = await _taskRepository.GetProjectKeyAsync(command.ProjectId, cancellationToken);
        if (string.IsNullOrWhiteSpace(projectKey))
            throw new KeyNotFoundException($"Project '{command.ProjectId}' was not found.");

        // Membership guard
        if (!_currentUser.IsSuperAdmin)
        {
            var projectIds = await _membershipRepository
                .GetUserProjectIdsAsync(currentUserId, cancellationToken);

            if (!projectIds.Contains(command.ProjectId))
                throw new ForbiddenAccessException(
                    "You must be a direct member of the project to import tasks.");
        }

        // Open workbook
        using var stream = new MemoryStream(command.FileBytes);
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch
        {
            result.Errors.Add(Error(0, "File", "The file could not be read. Ensure it is a valid .xlsx file."));
            return result;
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null)
            {
                result.Errors.Add(Error(0, "File", "The uploaded file contains no worksheets."));
                return result;
            }

            // Header validation
            var headerErrors = ValidateHeaders(sheet);
            if (headerErrors.Count > 0)
            {
                result.Errors.AddRange(headerErrors);
                return result;
            }

            // Read data rows
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow <= 1)
            {
                result.Errors.Add(Error(0, "File", "The uploaded file contains no task data."));
                return result;
            }

            var parsedRows = new List<ParsedImportRow>();
            for (var rowNum = 2; rowNum <= lastRow; rowNum++)
            {
                var row = sheet.Row(rowNum);
                if (row.IsEmpty()) continue;
                parsedRows.Add(ParseRow(row, rowNum));
            }

            if (parsedRows.Count == 0)
            {
                result.Errors.Add(Error(0, "File", "The uploaded file contains no task data."));
                return result;
            }

            // Build lookup tables
            var lookups = await BuildLookupsAsync(command.ProjectId, cancellationToken);

            // Validate all rows
            var seenTaskCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var parsed in parsedRows)
                result.Errors.AddRange(ValidateRow(parsed, lookups, seenTaskCodes));

            if (result.Errors.Count > 0)
                return result;

            var utcNow = DateTime.UtcNow;

            foreach (var parsed in parsedRows)
            {
                var status   = Enum.Parse<Status>(parsed.Status!, ignoreCase: true);
                var priority = Enum.Parse<TaskPriority>(parsed.Priority!, ignoreCase: true);
                var startDate = DateOnly.Parse(parsed.StartDate!);
                var endDate   = DateOnly.Parse(parsed.EndDate!);

                Guid? epicId = null;
                if (!string.IsNullOrWhiteSpace(parsed.EpicTitle)
                    && lookups.EpicsByTitle.TryGetValue(parsed.EpicTitle.Trim(), out var eid))
                    epicId = eid;

                Guid? sprintId = null;
                if (!string.IsNullOrWhiteSpace(parsed.SprintName)
                    && lookups.SprintsByName.TryGetValue(parsed.SprintName.Trim(), out var sprintLookup))
                    sprintId = sprintLookup.Id;

                string? assigneeId = null;
                if (!string.IsNullOrWhiteSpace(parsed.Assignee)
                    && lookups.ActiveMembersByAssignee.TryGetValue(parsed.Assignee.Trim(), out var aid))
                    assigneeId = aid;

                var newTask = TaskItem.Create(
                    command.ProjectId,
                    epicId, sprintId, assigneeId,
                    currentUserId,
                    parsed.Title!.Trim(),
                    string.IsNullOrWhiteSpace(parsed.Description) ? null : parsed.Description.Trim(),
                    status, priority,
                    startDate, endDate,
                    utcNow);

                // insert to obtain DB-assigned Id
                await _taskRepository.AddAsync(newTask, cancellationToken);

                // assign TaskCode now that Id is available
                newTask.AssignTaskCode(projectKey);
                await _taskRepository.UpdateAsync(newTask, cancellationToken);

                result.CreatedCount++;
            }
        }

        return result;
    }

    private sealed class ParsedImportRow
    {
        public int RowNumber { get; init; }
        public string? TaskCode { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? Status { get; init; }
        public string? Priority { get; init; }
        public string? StartDate { get; init; }
        public string? EndDate { get; init; }
        public string? EpicTitle { get; init; }
        public string? Assignee { get; init; }
        public string? SprintName { get; init; }
    }

    private static ParsedImportRow ParseRow(IXLRow row, int rowNumber) =>
        new()
        {
            RowNumber = rowNumber,
            TaskCode = GetCellText(row, 1),
            Title = GetCellText(row, 2),
            Description = GetCellText(row, 3),
            Status = GetCellText(row, 4),
            Priority = GetCellText(row, 5),
            StartDate = GetCellText(row, 6),
            EndDate = GetCellText(row, 7),
            EpicTitle = GetCellText(row, 8),
            Assignee = GetCellText(row, 9),
            SprintName = IsHeader(row.Worksheet.Row(1), 10, "SprintName") ? GetCellText(row, 10) : null,
        };

    private static string? GetCellText(IXLRow row, int col)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;

        // ClosedXML may parse date cells as DateTime — convert to string
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var text = cell.GetString().Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    // Header validation
    private static List<TaskImportValidationErrorDto> ValidateHeaders(IXLWorksheet sheet)
    {
        var errors = new List<TaskImportValidationErrorDto>();
        var headerRow = sheet.Row(1);

        for (var col = 1; col <= RequiredHeaderCount; col++)
        {
            var actual   = headerRow.Cell(col).GetString().Trim();
            var expected = ExpectedHeaders[col - 1];

            if (!HeaderMatches(expected, actual)
                && !(col == 9 && HeaderMatches("AssigneeEmail", actual)))
            {
                errors.Add(Error(1, $"Column {col}",
                    $"Expected header '{expected}' but found '{actual}'."));
            }
        }

        var tenthHeader = headerRow.Cell(10).GetString().Trim();
        if (!string.IsNullOrWhiteSpace(tenthHeader)
            && !HeaderMatches("SprintName", tenthHeader)
            && !HeaderMatches("Reporter", tenthHeader))
        {
            errors.Add(Error(1, "Column 10",
                $"Expected header 'SprintName' or 'Reporter' but found '{tenthHeader}'."));
        }

        return errors;
    }

    private static bool IsHeader(IXLRow headerRow, int col, string expected) =>
        HeaderMatches(expected, headerRow.Cell(col).GetString().Trim());

    private static bool HeaderMatches(string expected, string actual) =>
        NormalizeHeader(expected) == NormalizeHeader(actual);

    private static string NormalizeHeader(string value) =>
        new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

    // Lookup tables 

    private sealed class SprintLookupEntry
    {
        public Guid Id { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public SprintStatus Status { get; init; }
    }

    private sealed class ProjectLookups
    {
        public Guid ProjectId { get; init; }
        public Dictionary<string, Guid> EpicsByTitle { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, SprintLookupEntry> SprintsByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> ActiveMembersByAssignee { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        /// All task codes across the whole system — used to detect cross-project reuse.
        public Dictionary<string, Guid> AllTaskCodeProjectMap { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<ProjectLookups> BuildLookupsAsync(Guid projectId, CancellationToken ct)
    {
        // Epics belonging to the project
        var epicsRaw = await _epicRepository.Query()
            .Where(e => e.ProjectId == projectId)
            .Select(e => new { e.Title, e.Id })
            .ToListAsync(ct);

        var epicsByTitle = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in epicsRaw)
            epicsByTitle.TryAdd(e.Title, e.Id);

        // Sprints belonging to the project
        var sprintsRaw = await _sprintRepository.Query()
            .Where(s => s.ProjectId == projectId)
            .Select(s => new { s.Name, s.Id, s.StartDate, s.EndDate, s.Status })
            .ToListAsync(ct);

        var sprintsByName = new Dictionary<string, SprintLookupEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sprintsRaw)
            sprintsByName.TryAdd(s.Name, new SprintLookupEntry
            {
                Id = s.Id,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status,
            });

        // Active project members (non-archived) keyed by display name and email.
        var memberships = await _membershipRepository.GetProjectMembershipsAsync(projectId, ct);
        var activeMembersByAssignee = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var up in memberships)
        {
            if (!up.User.IsActive || up.User.IsArchived)
                continue;

            var fullName = $"{up.User.FirstName} {up.User.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                activeMembersByAssignee.TryAdd(fullName, up.UserId);

            if (up.User.IsActive && !up.User.IsArchived && !string.IsNullOrWhiteSpace(up.User.Email))
                activeMembersByAssignee.TryAdd(up.User.Email, up.UserId);
        }

        // All task codes globally (to detect cross-project conflicts)
        var allTaskCodes = await _taskRepository.Query()
            .Where(t => t.TaskCode != "")
            .Select(t => new { t.TaskCode, t.ProjectId })
            .ToListAsync(ct);

        var allTaskCodeProjectMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in allTaskCodes)
            allTaskCodeProjectMap.TryAdd(t.TaskCode, t.ProjectId);

        return new ProjectLookups
        {
            ProjectId = projectId,
            EpicsByTitle = epicsByTitle,
            SprintsByName = sprintsByName,
            ActiveMembersByAssignee = activeMembersByAssignee,
            AllTaskCodeProjectMap = allTaskCodeProjectMap,
        };
    }
    private static List<TaskImportValidationErrorDto> ValidateRow(
        ParsedImportRow row,
        ProjectLookups lookups,
        HashSet<string> seenTaskCodes)
    {
        var errors = new List<TaskImportValidationErrorDto>();
        var rn = row.RowNumber;

        // Title
        if (string.IsNullOrWhiteSpace(row.Title))
            errors.Add(Error(rn, "Title", "Title is required."));
        else if (row.Title.Trim().Length > 100)
            errors.Add(Error(rn, "Title", "Title exceeds 100 characters."));

        // Description
        if (!string.IsNullOrWhiteSpace(row.Description) && row.Description.Trim().Length > 500)
            errors.Add(Error(rn, "Description", "Description exceeds 500 characters."));

        // Status
        if (string.IsNullOrWhiteSpace(row.Status))
            errors.Add(Error(rn, "Status", "Status is required."));
        else if (!Enum.TryParse<Status>(row.Status.Trim(), ignoreCase: true, out _))
            errors.Add(Error(rn, "Status",
                $"Invalid status '{row.Status}'. Must be one of: {string.Join(", ", Enum.GetNames<Status>())}."));

        // Priority
        if (string.IsNullOrWhiteSpace(row.Priority))
            errors.Add(Error(rn, "Priority", "Priority is required."));
        else if (!Enum.TryParse<TaskPriority>(row.Priority.Trim(), ignoreCase: true, out _))
            errors.Add(Error(rn, "Priority",
                $"Invalid priority '{row.Priority}'. Must be one of: {string.Join(", ", Enum.GetNames<TaskPriority>())}."));

        // StartDate
        DateOnly parsedStart = default;
        if (string.IsNullOrWhiteSpace(row.StartDate))
            errors.Add(Error(rn, "StartDate", "StartDate is required."));
        else if (!DateOnly.TryParse(row.StartDate.Trim(), out parsedStart))
            errors.Add(Error(rn, "StartDate",
                $"Invalid date '{row.StartDate}'. Expected format: yyyy-MM-dd."));

        // EndDate
        DateOnly parsedEnd = default;
        if (string.IsNullOrWhiteSpace(row.EndDate))
            errors.Add(Error(rn, "EndDate", "EndDate is required."));
        else if (!DateOnly.TryParse(row.EndDate.Trim(), out parsedEnd))
            errors.Add(Error(rn, "EndDate",
                $"Invalid date '{row.EndDate}'. Expected format: yyyy-MM-dd."));

        // StartDate <= EndDate
        if (parsedStart != default && parsedEnd != default && parsedStart > parsedEnd)
            errors.Add(Error(rn, "StartDate", "StartDate must be earlier than or equal to EndDate."));

        // TaskCode — cross-project conflict and intra-file duplicate
        if (!string.IsNullOrWhiteSpace(row.TaskCode))
        {
            var code = row.TaskCode.Trim();

            // Duplicate within the same file
            if (!seenTaskCodes.Add(code))
                errors.Add(Error(rn, "TaskCode",
                    $"Duplicate TaskCode '{code}' in the import file."));

            if (lookups.AllTaskCodeProjectMap.ContainsKey(code))
                errors.Add(Error(rn, "TaskCode",
                    $"Task with TaskCode '{code}' already exists."));
        }

        // EpicTitle
        if (!string.IsNullOrWhiteSpace(row.EpicTitle)
            && !lookups.EpicsByTitle.ContainsKey(row.EpicTitle.Trim()))
            errors.Add(Error(rn, "EpicTitle",
                $"Epic '{row.EpicTitle.Trim()}' was not found in this project."));

        // SprintName
        if (!string.IsNullOrWhiteSpace(row.SprintName))
        {
            var sprintName = row.SprintName.Trim();
            if (!lookups.SprintsByName.TryGetValue(sprintName, out var sprint))
            {
                errors.Add(Error(rn, "SprintName",
                    $"Sprint '{sprintName}' was not found in this project."));
            }
            else
            {
                if (sprint.Status is SprintStatus.Completed or SprintStatus.Cancelled or SprintStatus.Archived)
                    errors.Add(Error(rn, "SprintName",
                        $"Cannot import tasks into a {sprint.Status} sprint."));

                if (parsedStart != default && parsedStart < sprint.StartDate)
                    errors.Add(Error(rn, "StartDate",
                        $"Task start date ({parsedStart}) is before sprint start date ({sprint.StartDate})."));

                if (parsedEnd != default && parsedEnd > sprint.EndDate)
                    errors.Add(Error(rn, "EndDate",
                        $"Task end date ({parsedEnd}) exceeds sprint end date ({sprint.EndDate})."));
            }
        }

        // Assignee
        if (!string.IsNullOrWhiteSpace(row.Assignee)
            && !lookups.ActiveMembersByAssignee.ContainsKey(row.Assignee.Trim()))
            errors.Add(Error(rn, "Assignee",
                $"Assignee '{row.Assignee.Trim()}' is not an active member of this project."));

        return errors;
    }

    private static TaskImportValidationErrorDto Error(int row, string field, string message) =>
        new() { RowNumber = row, Field = field, Message = message };
}
