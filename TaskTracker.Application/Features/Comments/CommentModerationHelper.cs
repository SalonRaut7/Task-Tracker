using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Comments;

/// <summary>
/// Shared helper for comment moderation role-hierarchy logic.
/// Used by both UpdateCommentCommandHandler and DeleteCommentCommandHandler.
/// </summary>
public static class CommentModerationHelper
{
    /// <summary>
    /// Determines if a user with a given role can moderate (edit/delete) a comment
    /// authored by a user with another role.
    /// Hierarchy: OrgAdmin > ProjectManager > Developer/QA/Viewer.
    /// </summary>
    public static bool CanModerateComment(string? userRole, string? authorRole)
    {
        if (userRole is null) return false;
        if (authorRole is null) return true; // Edge case: author not in scope, allow moderation

        // ProjectManager can moderate Developer, QA, Viewer
        if (string.Equals(userRole, AppRoles.ProjectManager, StringComparison.Ordinal))
        {
            return !string.Equals(authorRole, AppRoles.ProjectManager, StringComparison.Ordinal)
                && !string.Equals(authorRole, AppRoles.OrgAdmin, StringComparison.Ordinal);
        }

        // OrgAdmin can moderate anyone
        if (string.Equals(userRole, AppRoles.OrgAdmin, StringComparison.Ordinal))
        {
            return true;
        }

        // Developer, QA, Viewer cannot moderate others
        return false;
    }
}
