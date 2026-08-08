using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.ValueObjects;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentManagement.IntegrationTests;

/// <summary>
/// Collection fixture: builds the test API factory once per test run, resets the test
/// database to a clean schema and seeds the baseline users, class, subject and teacher
/// allocation shared by all integration tests. Tests within the collection run serially
/// and create their own records with unique business keys.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public TestApiFactory Factory { get; private set; } = null!;

    public const string AdminEmail = "admin.it@test.dev";
    public const string AdminPassword = "AdminPass123!";

    public const string TeacherEmail = "teacher.it@test.dev";
    public const string TeacherPassword = "TeacherPass123!";

    public const string OtherTeacherEmail = "teacher2.it@test.dev";
    public const string OtherTeacherPassword = "TeacherPass123!";

    public const string StudentEmail = "student.it@test.dev";
    public const string StudentPassword = "StudentPass123!";

    public const string SecondStudentEmail = "student2.it@test.dev";
    public const string SecondStudentPassword = "StudentPass123!";

    public Guid AdminId { get; private set; }
    public Guid ClassId { get; private set; }
    public Guid OtherClassId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid TeacherId { get; private set; }
    public Guid OtherTeacherId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid SecondStudentId { get; private set; }

    public async Task InitializeAsync()
    {
        Factory = new TestApiFactory();

        if (Directory.Exists(TestApiFactory.UploadsPath))
        {
            Directory.Delete(TestApiFactory.UploadsPath, recursive: true);
        }

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var klass = new Class("Integration Test Class", "Seeded for integration tests.");
        var otherClass = new Class("Integration Other Class", "Second seeded class.");
        var subject = new Subject("Mathematics", "IT-MATH");
        db.Classes.AddRange(klass, otherClass);
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        var admin = CreateUser(hasher, "IT Admin", AdminEmail, AdminPassword, UserRole.Admin, null);
        var teacher = CreateUser(hasher, "IT Teacher", TeacherEmail, TeacherPassword, UserRole.Teacher, null);
        var otherTeacher = CreateUser(hasher, "IT Teacher Two", OtherTeacherEmail, OtherTeacherPassword, UserRole.Teacher, null);
        var student = CreateUser(hasher, "IT Student", StudentEmail, StudentPassword, UserRole.Student, klass.Id);
        var secondStudent = CreateUser(hasher, "IT Student Two", SecondStudentEmail, SecondStudentPassword, UserRole.Student, klass.Id);

        db.Users.AddRange(admin, teacher, otherTeacher, student, secondStudent);
        await db.SaveChangesAsync();

        db.TeacherAssignments.AddRange(
            new TeacherAssignment(teacher.Id, klass.Id, subject.Id),
            new TeacherAssignment(teacher.Id, otherClass.Id, subject.Id));
        await db.SaveChangesAsync();

        AdminId = admin.Id;
        ClassId = klass.Id;
        OtherClassId = otherClass.Id;
        SubjectId = subject.Id;
        TeacherId = teacher.Id;
        OtherTeacherId = otherTeacher.Id;
        StudentId = student.Id;
        SecondStudentId = secondStudent.Id;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }

    private static User CreateUser(
        IPasswordHasher hasher,
        string fullName,
        string email,
        string password,
        UserRole role,
        Guid? classId)
    {
        var user = new User(fullName, new EmailAddress(email), role, classId, DateTimeOffset.UtcNow);
        user.SetPasswordHash(hasher.Hash(password), DateTimeOffset.UtcNow);
        return user;
    }
}
