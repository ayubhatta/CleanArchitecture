namespace CleanArchitecture.Application.Interfaces;

public interface IEmailService
{
    Task SendEnrollmentConfirmationAsync(string toEmail, string studentName, string courseName);
    Task SendWelcomeEmailAsync(string toEmail, string studentName);
    Task SendEnrollmentCancellationAsync(string toEmail, string studentName, string courseName);
    Task SendCourseUpdatedEmailAsync(string toEmail, string studentName, string courseName);
}