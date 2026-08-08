namespace AssignmentManagement.Application.Mapping;

/// <summary>
/// Keys for the lookup data handlers hand to AutoMapper through the mapping context
/// <see cref="AutoMapper.IMappingOperationOptions.Items"/> collection. Response DTOs
/// need display names (teacher, class, subject, assignment title, student name) that the
/// mapped aggregate does not hold; handlers supply them here so the same profile serves
/// both projections that can resolve the names and those that cannot.
/// </summary>
internal static class MapperContext
{
    /// <summary>A <see cref="IReadOnlyDictionary{Guid,String}"/> of class identifier to name.</summary>
    public const string ClassNames = nameof(ClassNames);

    /// <summary>A <see cref="IReadOnlyDictionary{Guid,String}"/> of subject identifier to name.</summary>
    public const string SubjectNames = nameof(SubjectNames);

    /// <summary>A <see cref="IReadOnlyDictionary{Guid,String}"/> of user identifier to full name.</summary>
    public const string TeacherNames = nameof(TeacherNames);

    /// <summary>A single resolved class name.</summary>
    public const string ClassName = nameof(ClassName);

    /// <summary>A single resolved subject name.</summary>
    public const string SubjectName = nameof(SubjectName);

    /// <summary>A single resolved assignment title.</summary>
    public const string AssignmentTitle = nameof(AssignmentTitle);

    /// <summary>A single resolved student name.</summary>
    public const string StudentName = nameof(StudentName);
}
