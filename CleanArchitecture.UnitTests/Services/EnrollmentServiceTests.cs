using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Events;
using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Application.Services;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using FluentAssertions;
using Moq;

namespace CleanArchitecture.UnitTests.Services;

public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _repositoryMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly EnrollmentService _service;

    public EnrollmentServiceTests()
    {
        _repositoryMock = new Mock<IEnrollmentRepository>();
        _publisherMock = new Mock<IMessagePublisher>();
        _service = new EnrollmentService(_repositoryMock.Object, _publisherMock.Object);
    }

    private static StudentCourse MakeEnrollment(int studentId = 1, int courseId = 1) => new()
    {
        StudentId = studentId,
        CourseId = courseId,
        EnrollmentDate = DateTime.UtcNow,
        Status = EnrollmentStatus.Enrolled,
        Student = new Student { StudentId = studentId, Name = "John", Email = "john@test.com" },
        Course = new Course { CourseId = courseId, CourseName = "C# Basics", Credits = 3 }
    };

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEnrollments()
    {
        var enrollments = new List<StudentCourse> { MakeEnrollment(1, 1), MakeEnrollment(2, 1) };
        _repositoryMock.Setup(r => r.FindAllAsync(false)).ReturnsAsync(enrollments);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByStudentIdAsync_ShouldReturnEnrollmentsForStudent()
    {
        var enrollments = new List<StudentCourse> { MakeEnrollment(1, 1), MakeEnrollment(1, 2) };
        _repositoryMock.Setup(r => r.GetByStudentIdAsync(1)).ReturnsAsync(enrollments);

        var result = await _service.GetByStudentIdAsync(1);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.StudentId == 1);
    }

    [Fact]
    public async Task GetByCourseIdAsync_ShouldReturnEnrollmentsForCourse()
    {
        var enrollments = new List<StudentCourse> { MakeEnrollment(1, 1), MakeEnrollment(2, 1) };
        _repositoryMock.Setup(r => r.GetByCourseIdAsync(1)).ReturnsAsync(enrollments);

        var result = await _service.GetByCourseIdAsync(1);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.CourseId == 1);
    }

    [Fact]
    public async Task CreateAsync_ShouldPublishEnrollmentCreatedEvent()
    {
        var dto = new CreateEnrollmentDto
        {
            StudentId = 1,
            CourseId = 1,
            EnrollmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Enrolled
        };

        var enrollment = MakeEnrollment(1, 1);
        _repositoryMock.Setup(r => r.Create(It.IsAny<StudentCourse>()));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(enrollment);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<EnrollmentCreatedEvent>()))
                      .Returns(Task.CompletedTask);

        await _service.CreateAsync(dto);

        _publisherMock.Verify(
            p => p.PublishAsync("enrollment.created", It.IsAny<EnrollmentCreatedEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenEnrollmentNotFound()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99, 99)).ReturnsAsync((StudentCourse?)null);

        var result = await _service.DeleteAsync(99, 99);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenEnrollmentExists()
    {
        var enrollment = MakeEnrollment(1, 1);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(enrollment);
        _repositoryMock.Setup(r => r.Delete(enrollment));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<EnrollmentCancelledEvent>()))
                      .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1, 1);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldPublishEnrollmentCancelledEvent()
    {
        var enrollment = MakeEnrollment(1, 1);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(enrollment);
        _repositoryMock.Setup(r => r.Delete(enrollment));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<EnrollmentCancelledEvent>()))
                      .Returns(Task.CompletedTask);

        await _service.DeleteAsync(1, 1);

        _publisherMock.Verify(
            p => p.PublishAsync("enrollment.cancelled", It.IsAny<EnrollmentCancelledEvent>()),
            Times.Once);
    }
}