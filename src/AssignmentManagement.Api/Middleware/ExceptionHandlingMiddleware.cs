using System.Text.Json;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Api.Middleware;

/// <summary>
/// Centralized exception handling. Maps known exception types to appropriate HTTP
/// responses using ProblemDetails (RFC 7807) and logs unexpected failures. Stack traces
/// are never returned to clients.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, errors) = MapException(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}.",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request {Method} {Path} failed with {Status}.",
                context.Request.Method, context.Request.Path, statusCode);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

    /// <summary>
    /// Maps a thrown exception to a ProblemDetails status. Validation failures and
    /// unknown-path errors are client errors (4xx), while business-rule violations,
    /// invalid state transitions and optimistic-concurrency conflicts all surface as
    /// 409 Conflict so a client can reload and retry; everything else is an opaque 500.
    /// </summary>
    private static (int Status, string Title, string Detail, object? Errors) MapException(Exception exception) =>
        exception switch
        {
            ValidationException validation =>
                (StatusCodes.Status400BadRequest,
                 "Validation failed",
                 validation.Message,
                 validation.Errors
                     .GroupBy(e => e.PropertyName)
                     .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

            NotFoundException notFound =>
                (StatusCodes.Status404NotFound, "Not found", notFound.Message, null),

            UnauthorizedException unauthorized =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", unauthorized.Message, null),

            ForbiddenException forbidden =>
                (StatusCodes.Status403Forbidden, "Forbidden", forbidden.Message, null),

            InvalidStateTransition transition =>
                (StatusCodes.Status409Conflict, "Conflict", transition.Message, null),

            BusinessRuleViolation business =>
                (StatusCodes.Status409Conflict, "Conflict", business.Message, null),

            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict,
                 "Conflict",
                 "The record was modified by someone else. Reload and try again.",
                 null),

            _ =>
                (StatusCodes.Status500InternalServerError,
                 "An unexpected error occurred",
                 "An error occurred while processing your request.",
                 null),
        };
}
