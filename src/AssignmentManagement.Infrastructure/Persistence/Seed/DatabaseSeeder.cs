using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.ValueObjects;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds realistic development data so every role has something to work with:
/// users for all three roles, classes, subjects, a teacher allocation, published and
/// draft assignments, and sample submissions in each state.
/// </summary>
/// <remarks>
/// The seeder is idempotent: it looks up existing records by their business key (email,
/// name, code) before creating anything, so it can safely run on every startup.
/// Demo credentials are development-only and documented in the README. The passwords
/// below are hashed at seed time; nothing plaintext is stored.
/// </remarks>
public sealed class DatabaseSeeder
{
    private readonly WriteDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        WriteDbContext db,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var gradeSeven = await EnsureClassAsync("Grade 7", "Secondary section A", cancellationToken);
        var gradeEight = await EnsureClassAsync("Grade 8", "Secondary section B", cancellationToken);

        var mathematics = await EnsureSubjectAsync("Mathematics", "MATH-101", cancellationToken);
        var english = await EnsureSubjectAsync("English", "ENG-102", cancellationToken);
        var science = await EnsureSubjectAsync("Science", "SCI-103", cancellationToken);

        var admin = await EnsureUserAsync("System Administrator", "admin@school.edu", "Admin@123", UserRole.Admin, null, cancellationToken);
        var teacher = await EnsureUserAsync("Rafiq Ahmed", "teacher@school.edu", "Teacher@123", UserRole.Teacher, null, cancellationToken);
        var student = await EnsureUserAsync("Nusrat Jahan", "student@school.edu", "Student@123", UserRole.Student, gradeSeven.Id, cancellationToken);
        var secondStudent = await EnsureUserAsync("Tanvir Hasan", "student2@school.edu", "Student@123", UserRole.Student, gradeSeven.Id, cancellationToken);

        await EnsureTeacherAssignmentAsync(teacher, gradeSeven, mathematics, cancellationToken);
        await EnsureTeacherAssignmentAsync(teacher, gradeSeven, english, cancellationToken);
        await EnsureTeacherAssignmentAsync(teacher, gradeEight, science, cancellationToken);

        await EnsureAssignmentAsync(
            teacher,
            gradeSeven,
            mathematics,
            "Quadratic Equations Practice",
            "Solve exercises 1 to 20 from chapter 7 and show full working for every step.",
            now.AddDays(7),
            100,
            AssignmentStatus.Published,
            cancellationToken);

        await EnsureAssignmentAsync(
            teacher,
            gradeSeven,
            english,
            "Book Review Essay",
            "Write a 500-word review of the novel we studied this term.",
            now.AddDays(10),
            50,
            AssignmentStatus.Published,
            cancellationToken);

        var draft = await EnsureAssignmentAsync(
            teacher,
            gradeEight,
            science,
            "Motion and Force: Chapter Notes",
            "Prepare structured notes on Newton's laws of motion.",
            now.AddDays(14),
            100,
            AssignmentStatus.Draft,
            cancellationToken);

        await EnsureSubmissionAsync(
            student,
            "Quadratic Equations Practice",
            "x^2 - 5x + 6 = 0 factors to (x-2)(x-3)=0, so x = 2 or x = 3. Full working attached.",
            null,
            null,
            cancellationToken);

        await EnsureSubmissionAsync(
            secondStudent,
            "Quadratic Equations Practice",
            "I solved using the quadratic formula. x = [5 ± sqrt(1)]/2 giving x = 3 and x = 2.",
            null,
            null,
            cancellationToken);

        await EnsureSubmissionAsync(
            student,
            "Book Review Essay",
            "The novel explores identity through the eyes of its teenage narrator, and the final chapter reframes the entire story.",
            45,
            "Excellent analysis. Expand the conclusion and link the theme to the chapter on narrative voice.",
            cancellationToken);

        _logger.LogInformation("Database seeding complete. Demo credentials: admin@school.edu / Admin@123, teacher@school.edu / Teacher@123, student@school.edu / Student@123");
    }

    private async Task<Class> EnsureClassAsync(string name, string description, CancellationToken ct)
    {
        var existing = await _db.Classes.SingleOrDefaultAsync(c => c.Name == name, ct);
        if (existing is not null)
        {
            return existing;
        }

        var klass = new Class(name, description);
        _db.Classes.Add(klass);
        await _db.SaveChangesAsync(ct);
        return klass;
    }

    private async Task<Subject> EnsureSubjectAsync(string name, string code, CancellationToken ct)
    {
        var existing = await _db.Subjects.SingleOrDefaultAsync(s => s.Code == code, ct);
        if (existing is not null)
        {
            return existing;
        }

        var subject = new Subject(name, code);
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync(ct);
        return subject;
    }

    private async Task<User> EnsureUserAsync(
        string fullName,
        string email,
        string password,
        UserRole role,
        Guid? classId,
        CancellationToken ct)
    {
        var existing = await _db.Users.SingleOrDefaultAsync(u => u.Email == new EmailAddress(email), ct);
        if (existing is not null)
        {
            return existing;
        }

        var user = new User(fullName, new EmailAddress(email), role, classId, DateTimeOffset.UtcNow);
        user.SetPasswordHash(_passwordHasher.Hash(password), DateTimeOffset.UtcNow);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    private async Task EnsureTeacherAssignmentAsync(
        User teacher,
        Class klass,
        Subject subject,
        CancellationToken ct)
    {
        var exists = await _db.TeacherAssignments.AnyAsync(
            t => t.TeacherId == teacher.Id && t.ClassId == klass.Id && t.SubjectId == subject.Id,
            ct);

        if (!exists)
        {
            _db.TeacherAssignments.Add(new TeacherAssignment(teacher.Id, klass.Id, subject.Id));
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<Assignment> EnsureAssignmentAsync(
        User teacher,
        Class klass,
        Subject subject,
        string title,
        string description,
        DateTimeOffset deadline,
        decimal maximumMarks,
        AssignmentStatus status,
        CancellationToken ct)
    {
        var existing = await _db.Assignments.SingleOrDefaultAsync(
            a => a.TeacherId == teacher.Id && a.Title == title,
            ct);

        if (existing is not null)
        {
            return existing;
        }

        var assignment = new Assignment(
            teacher.Id,
            klass.Id,
            subject.Id,
            title,
            description,
            deadline,
            maximumMarks,
            DateTimeOffset.UtcNow);

        if (status == AssignmentStatus.Published)
        {
            assignment.Publish(DateTimeOffset.UtcNow);
        }

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(ct);
        return assignment;
    }

    private async Task EnsureSubmissionAsync(
        User student,
        string assignmentTitle,
        string answer,
        decimal? marks,
        string? feedback,
        CancellationToken ct)
    {
        var assignment = await _db.Assignments.SingleOrDefaultAsync(
            a => a.Title == assignmentTitle,
            ct);

        if (assignment is null)
        {
            return;
        }

        var exists = await _db.Submissions.AnyAsync(
            s => s.AssignmentId == assignment.Id && s.StudentId == student.Id,
            ct);

        if (exists)
        {
            return;
        }

        var submission = Submission.Create(
            assignment.Id,
            student.Id,
            answer,
            assignmentPublished: true,
            assignment.Deadline,
            DateTimeOffset.UtcNow);

        if (marks is not null)
        {
            submission.Grade(
                assignment.TeacherId,
                assignment.TeacherId,
                assignment.MaximumMarks,
                marks.Value,
                feedback,
                DateTimeOffset.UtcNow);
        }

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync(ct);
    }
}
