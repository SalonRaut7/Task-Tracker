using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Comments.Queries.GetCommentById;

public sealed class GetCommentByIdQueryHandler : IRequestHandler<GetCommentByIdQuery, CommentDto?>
{
    private readonly ICommentRepository _commentRepository;

    public GetCommentByIdQueryHandler(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<CommentDto?> Handle(GetCommentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _commentRepository.Query()
            .Where(comment => comment.Id == request.Id)
            .Select(CommentDtoMapper.Projection())
            .FirstOrDefaultAsync(cancellationToken);
    }
}
