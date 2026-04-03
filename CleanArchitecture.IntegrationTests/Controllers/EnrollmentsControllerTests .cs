using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Enums;
using FluentAssertions;

namespace CleanArchitecture.IntegrationTests.Controllers;

public class EnrollmentsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<StudentDto> CreateStudentAsync(string name = "John", string email = "john@test.com")
    {
        var dto = new CreateStudentDto { Name = name, Email = email, Status = StudentStatus.Active };
        var response = await _client.PostAsJsonAsync("/api/students", dto);
        return (await response.Content.ReadFromJsonAsync<StudentDto>())!;
    }

    private async Task<CourseDto> CreateCourseAsync(string name = "C# Basics")
    {
        var dto = new CreateCourseDto { CourseName = name, Credits = 3, Level = CourseLevel.Beginner };
        var response = await _client.PostAsJsonAsync("/api/courses", dto);
        return (await response.Content.ReadFromJsonAsync<CourseDto>())!;
    }

    [Fact]
    public async Task GetAll_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/enrollments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WithCreatedEnrollment()
    {
        var student = await CreateStudentAsync("Alice", "alice@test.com");
        var course = await CreateCourseAsync("Clean Architecture");

        var dto = new CreateEnrollmentDto
        {
            StudentId = student.StudentId,
            CourseId = course.CourseId,
            EnrollmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Enrolled
        };

        var response = await _client.PostAsJsonAsync("/api/enrollments", dto);
        var created = await response.Content.ReadFromJsonAsync<EnrollmentDto>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Should().NotBeNull();
        created!.StudentId.Should().Be(student.StudentId);
        created.CourseId.Should().Be(course.CourseId);
        created.StudentName.Should().Be("Alice");
        created.CourseName.Should().Be("Clean Architecture");
    }

    [Fact]
    public async Task GetByStudent_ShouldReturn200_WithEnrollments()
    {
        var student = await CreateStudentAsync("Bob", "bob@test.com");
        var course = await CreateCourseAsync("RabbitMQ Basics");

        var dto = new CreateEnrollmentDto
        {
            StudentId = student.StudentId,
            CourseId = course.CourseId,
            EnrollmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Enrolled
        };
        await _client.PostAsJsonAsync("/api/enrollments", dto);

        var response = await _client.GetAsync($"/api/enrollments/student/{student.StudentId}");
        var enrollments = await response.Content.ReadFromJsonAsync<IEnumerable<EnrollmentDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        enrollments.Should().NotBeEmpty();
        enrollments!.Should().OnlyContain(e => e.StudentId == student.StudentId);
    }

    [Fact]
    public async Task GetByCourse_ShouldReturn200_WithEnrollments()
    {
        var student = await CreateStudentAsync("Carol", "carol@test.com");
        var course = await CreateCourseAsync("Redis Caching");

        var dto = new CreateEnrollmentDto
        {
            StudentId = student.StudentId,
            CourseId = course.CourseId,
            EnrollmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Enrolled
        };
        await _client.PostAsJsonAsync("/api/enrollments", dto);

        var response = await _client.GetAsync($"/api/enrollments/course/{course.CourseId}");
        var enrollments = await response.Content.ReadFromJsonAsync<IEnumerable<EnrollmentDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        enrollments.Should().NotBeEmpty();
        enrollments!.Should().OnlyContain(e => e.CourseId == course.CourseId);
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenEnrollmentExists()
    {
        var student = await CreateStudentAsync("Dave", "dave@test.com");
        var course = await CreateCourseAsync("Docker Basics");

        var dto = new CreateEnrollmentDto
        {
            StudentId = student.StudentId,
            CourseId = course.CourseId,
            EnrollmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Enrolled
        };
        await _client.PostAsJsonAsync("/api/enrollments", dto);

        var response = await _client.DeleteAsync($"/api/enrollments/{student.StudentId}/{course.CourseId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenEnrollmentDoesNotExist()
    {
        var response = await _client.DeleteAsync("/api/enrollments/9999/9999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}