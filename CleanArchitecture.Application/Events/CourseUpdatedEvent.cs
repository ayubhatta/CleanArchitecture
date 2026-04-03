namespace CleanArchitecture.Application.Events;

public class CourseUpdatedEvent
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public List<EnrolledStudentInfo> EnrolledStudents { get; set; } = [];
}

public class EnrolledStudentInfo
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
}