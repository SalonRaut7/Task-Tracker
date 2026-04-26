using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Authorization;

namespace TaskTracker.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            if (context.Response.HasStarted)
                throw;

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            _logger.LogWarning(
                ex,
                "Validation exception for {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            var details = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Instance = context.Request.Path
            };

            details.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(details);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteProblemDetailsAsync(
                context,
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found.",
                exception: ex,
                logLevel: LogLevel.Warning);
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblemDetailsAsync(
                context,
                statusCode: StatusCodes.Status400BadRequest,
                title: "The request could not be processed.",
                exception: ex,
                logLevel: LogLevel.Warning);
            // The exception message is user-facing for validation/business-rule failures.
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemDetailsAsync(
                context,
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication is required.",
                exception: ex,
                logLevel: LogLevel.Warning,
                includeExceptionMessage: true);
        }
        catch (ForbiddenAccessException ex)
        {
            await WriteProblemDetailsAsync(
                context,
                statusCode: StatusCodes.Status403Forbidden,
                title: "You do not have permission to perform this action.",
                exception: ex,
                logLevel: LogLevel.Warning,
                includeExceptionMessage: true);
        }
        catch (DbUpdateException ex)
        {
            await WriteProblemDetailsAsync(
                context,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "A database error occurred while processing the request.",
                exception: ex,
                logLevel: LogLevel.Error);
        }
        catch (Exception ex)
        {
            await WriteProblemDetailsAsync(
                context,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred.",
                exception: ex,
                logLevel: LogLevel.Error);
        }
    }

    private async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        Exception exception,
        LogLevel logLevel,
        bool includeExceptionMessage = false)
    {
        if (context.Response.HasStarted)
            throw exception;

        _logger.Log(
            logLevel,
            exception,
            "Unhandled exception for {Method} {Path}. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);

        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = includeExceptionMessage || exception is InvalidOperationException
                ? exception.Message
                : null,
            Instance = context.Request.Path
        };

        details.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(details);
    }
}