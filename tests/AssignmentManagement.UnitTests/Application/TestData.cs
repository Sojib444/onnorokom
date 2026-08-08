using AssignmentManagement.Application;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.ValueObjects;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssignmentManagement.UnitTests.Application;

/// <summary>Shared factories for building aggregate instances used across handler tests.</summary>
internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A real AutoMapper instance wired up to the application's mapping profile, so the
    /// handler tests exercise the same DTO projection production uses.
    /// </summary>
    public static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddMaps(typeof(DependencyInjection).Assembly),
            NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }

    public static Class AClass(string name = "Grade 10") => new(name, null);

    public static Subject ASubject(string name = "Mathematics", string code = "MATH-101") => new(name, code);

    public static User ATeacher(
        string name = "Rafiq Ahmed",
        string email = "teacher@school.edu") =>
        BuildUser(name, email, UserRole.Teacher, null);

    public static User AStudent(
        string name = "Nusrat Jahan",
        string email = "student@school.edu",
        Guid? classId = null) =>
        BuildUser(name, email, UserRole.Student, classId);

    public static User AnAdmin(
        string name = "Sadia Rahman",
        string email = "admin@school.edu") =>
        BuildUser(name, email, UserRole.Admin, null);

    private static User BuildUser(
        string name,
        string email,
        UserRole role,
        Guid? classId)
    {
        var user = new User(name, new EmailAddress(email), role, classId, Now);
        user.SetPasswordHash("hash", Now);
        return user;
    }

    public static Assignment AnAssignment(
        Guid teacherId,
        Guid classId,
        Guid subjectId,
        DateTimeOffset? deadline = null,
        decimal maximumMarks = 100) =>
        new(
            teacherId,
            classId,
            subjectId,
            "Algebra Test",
            "Solve chapter 8 problems.",
            deadline ?? DateTimeOffset.UtcNow.AddDays(14),
            maximumMarks,
            Now);

    public static Submission ASubmission(
        Guid assignmentId,
        Guid studentId,
        DateTimeOffset deadline,
        string answer = "x = 4",
        bool published = true) =>
        Submission.Create(assignmentId, studentId, answer, published, deadline, Now);
}
