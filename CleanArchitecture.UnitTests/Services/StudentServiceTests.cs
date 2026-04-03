using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Events;
using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Application.Services;
using CleanArchitecture.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CleanArchitecture.UnitTests.Services;

public class StudentServiceTests
{
    private readonly Mock<IStudentRepository> _repositoryMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly StudentService _service;

    public StudentServiceTests()
    {
        _repositoryMock = new Mock<IStudentRepository>();
        _publisherMock = new Mock<IMessagePublisher>();
        _service = new StudentService(_repositoryMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnStudent_WhenStudentExists()
    {
        var student = new Student { StudentId = 1, Name = "John", Email = "john@test.com", Status = 0 };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.StudentId.Should().Be(1);
        result.Name.Should().Be("John");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenStudentDoesNotExist()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Student?)null);

        var result = await _service.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedStudent()
    {
        var dto = new CreateStudentDto { Name = "Jane", Email = "jane@test.com", Status = 0 };

        _repositoryMock.Setup(r => r.Create(It.IsAny<Student>()));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<StudentRegisteredEvent>()))
                      .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.Name.Should().Be("Jane");
        result.Email.Should().Be("jane@test.com");
    }

    [Fact]
    public async Task CreateAsync_ShouldPublishStudentRegisteredEvent()
    {
        var dto = new CreateStudentDto { Name = "Jane", Email = "jane@test.com", Status = 0 };

        _repositoryMock.Setup(r => r.Create(It.IsAny<Student>()));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<StudentRegisteredEvent>()))
                      .Returns(Task.CompletedTask);

        await _service.CreateAsync(dto);

        _publisherMock.Verify(
            p => p.PublishAsync("student.registered", It.IsAny<StudentRegisteredEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenStudentNotFound()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Student?)null);

        var result = await _service.DeleteAsync(99);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenStudentExists()
    {
        var student = new Student { StudentId = 1, Name = "John", Email = "john@test.com" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
        _repositoryMock.Setup(r => r.Delete(student));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<AuditEvent>()))
                      .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        result.Should().BeTrue();
    }
}