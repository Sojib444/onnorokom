using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Domain.Entities;
using AutoMapper;

namespace AssignmentManagement.Application.Mapping;

/// <summary>
/// AutoMapper configuration for the API response DTOs. Simple DTOs map by convention;
/// DTOs that carry resolved display names read them from the mapping context items (see
/// <see cref="MapperContext"/>), falling back to any navigation loaded on the aggregate.
/// </summary>
internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Class, ClassResponse>();

        CreateMap<Subject, SubjectResponse>();

        CreateMap<User, AuthenticatedUserResponse>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<User, UserResponse>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.ClassName, opt => opt.MapFrom(ResolveClassFromLookup))
            .ForCtorParam("ClassName", opt => opt.MapFrom(
                (User source, ResolutionContext context) =>
                    source.ClassId is Guid classId
                        ? ResolveFromLookup(context, MapperContext.ClassNames, classId)
                        : null));

        CreateMap<Assignment, AssignmentResponse>()
            .ForMember(dest => dest.ClassName, opt => opt.MapFrom(ResolveClassName))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(ResolveSubjectName))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<SubmissionAttachment, SubmissionAttachmentResponse>();

        CreateMap<Submission, SubmissionResponse>()
            .ForMember(dest => dest.AssignmentTitle, opt => opt.MapFrom(
                (src, _, _, context) => ResolveScalar(context, MapperContext.AssignmentTitle)))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(
                (src, _, _, context) => ResolveScalar(context, MapperContext.StudentName)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForCtorParam("AssignmentTitle", opt => opt.MapFrom(
                (Submission src, ResolutionContext context) =>
                    ResolveScalar(context, MapperContext.AssignmentTitle)))
            .ForCtorParam("StudentName", opt => opt.MapFrom(
                (Submission src, ResolutionContext context) =>
                    ResolveScalar(context, MapperContext.StudentName)));

        CreateMap<TeacherAssignment, TeacherAssignmentResponse>()
            .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(
                (src, _, _, context) => ResolveFromLookup(
                    context, MapperContext.TeacherNames, src.TeacherId) ?? "Unknown teacher"))
            .ForMember(dest => dest.ClassName, opt => opt.MapFrom(
                (src, _, _, context) => ResolveFromLookup(
                    context, MapperContext.ClassNames, src.ClassId) ?? "Unknown class"))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(
                (src, _, _, context) => ResolveFromLookup(
                    context, MapperContext.SubjectNames, src.SubjectId) ?? "Unknown subject"))
            .ForCtorParam("TeacherName", opt => opt.MapFrom(
                (TeacherAssignment src, ResolutionContext context) =>
                    ResolveFromLookup(context, MapperContext.TeacherNames, src.TeacherId)
                    ?? "Unknown teacher"))
            .ForCtorParam("ClassName", opt => opt.MapFrom(
                (TeacherAssignment src, ResolutionContext context) =>
                    ResolveFromLookup(context, MapperContext.ClassNames, src.ClassId)
                    ?? "Unknown class"))
            .ForCtorParam("SubjectName", opt => opt.MapFrom(
                (TeacherAssignment src, ResolutionContext context) =>
                    ResolveFromLookup(context, MapperContext.SubjectNames, src.SubjectId)
                    ?? "Unknown subject"));
    }

    /// <summary>Resolves a class name from the <see cref="MapperContext.ClassNames"/> lookup.</summary>
    private static string? ResolveClassFromLookup(
        User source,
        UserResponse destination,
        string? member,
        ResolutionContext context) =>
        source.ClassId is Guid classId
            ? ResolveFromLookup(context, MapperContext.ClassNames, classId)
            : null;

    /// <summary>
    /// Resolves an assignment's class name, preferring the single resolved value from the
    /// context and falling back to the loaded <see cref="Assignment.Class"/> navigation.
    /// </summary>
    private static string? ResolveClassName(
        Assignment source,
        AssignmentResponse destination,
        string? member,
        ResolutionContext context) =>
        ResolveScalar(context, MapperContext.ClassName) ?? source.Class?.Name;

    /// <summary>
    /// Resolves an assignment's subject name, preferring the single resolved value from
    /// the context and falling back to the loaded <see cref="Assignment.Subject"/> navigation.
    /// </summary>
    private static string? ResolveSubjectName(
        Assignment source,
        AssignmentResponse destination,
        string? member,
        ResolutionContext context) =>
        ResolveScalar(context, MapperContext.SubjectName) ?? source.Subject?.Name;

    private static string? ResolveScalar(ResolutionContext context, string key) =>
        context.TryGetItems(out var items) && items.TryGetValue(key, out var value)
            ? value as string
            : null;

    private static string? ResolveFromLookup(ResolutionContext context, string key, Guid id) =>
        context.TryGetItems(out var items)
            && items.TryGetValue(key, out var value)
            && value is IReadOnlyDictionary<Guid, string> lookup
            && lookup.TryGetValue(id, out var name)
                ? name
                : null;
}
