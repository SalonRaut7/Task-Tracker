using System;
using FluentValidation.TestHelper;
using TaskTracker.Application.Features.Tasks.Commands.CreateTask;
using TaskTracker.Domain.Enums;
using Xunit;

namespace TaskTracker.Tests.TaskTracker.Application.UnitTests.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator;

    public CreateTaskCommandValidatorTests()
    {
        _validator = new CreateTaskCommandValidator();
    }

    private static CreateTaskCommand CreateValidCommand()
    {
        return new CreateTaskCommand
        {
            ProjectId = Guid.NewGuid(),
            AssigneeId = "User-1",
            Title = "Test Task",
            Description = "This is a test task.",
            Status = Status.NotStarted,
            Priority = TaskPriority.Medium,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };
    }

    [Fact]
    public void Should_Have_Valid_Values()
    {
        //Arrange = setup test data and dependencies
        var command = CreateValidCommand();
        
        //Act = execute the thing being tested
        var result = _validator.TestValidate(command);
        
        //Assert = verify the expected outcome
        result.ShouldNotHaveAnyValidationErrors();
    }
}
