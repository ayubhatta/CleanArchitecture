using System.Net;
using System.Net.Mail;
using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Messaging;

public class EmailService(
    IOptions<EmailSettings> options,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendEnrollmentConfirmationAsync(string toEmail, string studentName, string courseName)
        => await SendAsync(toEmail, "Enrollment Confirmation", $"""
            Hi {studentName},

            You have been successfully enrolled in: {courseName}.

            Thank you for joining us!

            Regards,
            {_settings.DisplayName}
            """);

    public async Task SendWelcomeEmailAsync(string toEmail, string studentName)
        => await SendAsync(toEmail, "Welcome!", $"""
            Hi {studentName},

            Welcome! Your account has been successfully created.

            We're glad to have you on board.

            Regards,
            {_settings.DisplayName}
            """);

    public async Task SendEnrollmentCancellationAsync(string toEmail, string studentName, string courseName)
        => await SendAsync(toEmail, "Enrollment Cancelled", $"""
            Hi {studentName},

            Your enrollment in {courseName} has been cancelled.

            If this was a mistake, please re-enroll at your earliest convenience.

            Regards,
            {_settings.DisplayName}
            """);

    public async Task SendCourseUpdatedEmailAsync(string toEmail, string studentName, string courseName)
        => await SendAsync(toEmail, "Course Updated", $"""
            Hi {studentName},

            The course you are enrolled in has been updated: {courseName}.

            Please check the latest course details.

            Regards,
            {_settings.DisplayName}
            """);

    private async Task SendAsync(string toEmail, string subject, string body)
    {
        try
        {
            var from = new MailAddress(_settings.Email, _settings.DisplayName);
            var to = new MailAddress(toEmail);

            using var message = new MailMessage(from, to)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            using var smtp = new SmtpClient(_settings.Host, _settings.Port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_settings.Email, _settings.Password),
                EnableSsl = _settings.UseSSL,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            await smtp.SendMailAsync(message);

            logger.LogInformation("EMAIL SENT: To={Email}, Subject={Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EMAIL FAILED: To={Email}, Subject={Subject}", toEmail, subject);
        }
    }
}