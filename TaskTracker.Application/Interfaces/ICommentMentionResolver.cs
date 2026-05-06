namespace TaskTracker.Application.Interfaces;

public interface ICommentMentionResolver
{
    Task<IReadOnlyList<string>> ResolveMentionedUserIdsAsync(
        int taskId,
        string content,
        IEnumerable<string> explicitMentionedUserIds,
        string currentUserId,
        CancellationToken cancellationToken = default);
}
