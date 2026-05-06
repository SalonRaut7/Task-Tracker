using System.Text.RegularExpressions;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Domain.ReadModels;

namespace TaskTracker.Application.Services;

public sealed class CommentMentionResolver : ICommentMentionResolver
{
    private readonly ICommentRepository _commentRepository;

    public CommentMentionResolver(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<IReadOnlyList<string>> ResolveMentionedUserIdsAsync(
        int taskId,
        string content,
        IEnumerable<string> explicitMentionedUserIds,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var mentionableUsers = await _commentRepository.GetMentionableUsersForTaskAsync(
            taskId, cancellationToken);

        if (mentionableUsers.Count == 0)
        {
            return [];
        }

        var mentionableIds = mentionableUsers
            .Select(user => user.UserId)
            .ToHashSet(StringComparer.Ordinal);

        var resolvedIds = explicitMentionedUserIds
            .Where(id => mentionableIds.Contains(id))
            .ToHashSet(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(content) && content.Contains('@'))
        {
            foreach (var mentionableUser in mentionableUsers
                .OrderByDescending(user => GetFullName(user).Length))
            {
                var fullName = GetFullName(mentionableUser);
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    continue;
                }

                var pattern = $@"(?<!\S)@{Regex.Escape(fullName)}(?=$|\s|[.,!?;:)])";
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    resolvedIds.Add(mentionableUser.UserId);
                }
            }
        }

        resolvedIds.Remove(currentUserId);
        return resolvedIds.ToList();
    }

    private static string GetFullName(CommentMentionableUser user)
        => string.Concat(user.FirstName, " ", user.LastName).Trim();
}
