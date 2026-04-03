namespace CleanArchitecture.Application.Events;

public class StudentRegisteredEvent
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
}