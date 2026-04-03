using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Events;
using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Application.Services;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using FluentAssertions;
using Moq;

namespace CleanArchitecture.UnitTests.Services;

public class CourseServiceTests
{
    private readonly Mock<ICourseRepository> _repositoryMock;
    private readonly Mock<IEnrollmentRepository> _enrollmentRepositoryMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly CourseService _service;

    public CourseServiceTests()
    {
        _repositoryMock = new Mock<ICourseRepository>();
        _enrollmentRepositoryMock = new Mock<IEnrollmentRepository>();
        _publisherMock = new Mock<IMessagePublisher>();
        _service = new CourseService(_repositoryMock.Object, _enrollmentRepositoryMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCourse_WhenCourseExists()
    {
        // Arrange
        var course = new Course { CourseId = 1, CourseName = "C# Basics", Credits = 3, Level = CourseLevel.Beginner };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(course);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.CourseId.Should().Be(1);
        result.CourseName.Should().Be("C# Basics");
        result.Credits.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCourseDoesNotExist()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Course?)null);

        // Act
        var result = await _service.GetByIdAsync(99);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCourses()
    {
        // Arrange
        var courses = new List<Course>
        {
            new() { CourseId = 1, CourseName = "C# Basics", Credits = 3, Level = CourseLevel.Beginner },
            new() { CourseId = 2, CourseName = "Advanced C#", Credits = 5, Level = CourseLevel.Advanced }
        };
        _repositoryMock.Setup(r => r.FindAllAsync(false)).ReturnsAsync(courses);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.CourseName == "C# Basics");
        result.Should().Contain(c => c.CourseName == "Advanced C#");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedCourse()
    {
        // Arrange
        var dto = new CreateCourseDto { CourseName = "Clean Code", Credits = 4, Level = CourseLevel.Intermediate };
        _repositoryMock.Setup(r => r.Create(It.IsAny<Course>()));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<AuditEvent>()))
                      .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.CourseName.Should().Be("Clean Code");
        result.Credits.Should().Be(4);
        result.Level.Should().Be(CourseLevel.Intermediate);
    }

    [Fact]
    public async Task CreateAsync_ShouldPublishAuditEvent()
    {
        // Arrange
        var dto = new CreateCourseDto { CourseName = "Clean Code", Credits = 4, Level = CourseLevel.Intermediate };
        _repositoryMock.Setup(r => r.Create(It.IsAny<Course>()));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<AuditEvent>()))
                      .Returns(Task.CompletedTask);

        // Act
        await _service.CreateAsync(dto);

        // Assert
        _publisherMock.Verify(
            p => p.PublishAsync("audit.log", It.IsAny<AuditEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenCourseNotFound()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Course?)null);

        // Act
        var result = await _service.UpdateAsync(99, new UpdateCourseDto());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPublishCourseUpdatedEvent()
    {
        // Arrange
        var course = new Course { CourseId = 1, CourseName = "Old Name", Credits = 3, Level = CourseLevel.Beginner };
        var dto = new UpdateCourseDto { CourseName = "New Name", Credits = 5, Level = CourseLevel.Advanced };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(course);
        _repositoryMock.Setup(r => r.Update(It.IsAny<Course>()));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _enrollmentRepositoryMock.Setup(r => r.GetByCourseIdAsync(1))
                                 .ReturnsAsync(new List<StudentCourse>());
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<CourseUpdatedEvent>()))
                      .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateAsync(1, dto);

        // Assert
        _publisherMock.Verify(
            p => p.PublishAsync("course.updated", It.IsAny<CourseUpdatedEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenCourseNotFound()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Course?)null);

        // Act
        var result = await _service.DeleteAsync(99);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenCourseExists()
    {
        // Arrange
        var course = new Course { CourseId = 1, CourseName = "C# Basics", Credits = 3 };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(course);
        _repositoryMock.Setup(r => r.Delete(course));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<AuditEvent>()))
                      .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
    }
}