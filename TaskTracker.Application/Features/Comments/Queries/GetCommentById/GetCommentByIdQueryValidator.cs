using FluentValidation;

namespace TaskTracker.Application.Features.Comments.Queries.GetCommentById;

public sealed class GetCommentByIdQueryValidator : AbstractValidator<GetCommentByIdQuery>
{
    public GetCommentByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
