using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Enums;
using FluentAssertions;

namespace CleanArchitecture.IntegrationTests.Controllers;

public class StudentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StudentsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/students");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenStudentDoesNotExist()
    {
        var response = await _client.GetAsync("/api/students/9999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WithCreatedStudent()
    {
        var dto = new CreateStudentDto { Name = "John", Email = "john@test.com", Status = StudentStatus.Active };

        var response = await _client.PostAsJsonAsync("/api/students", dto);
        var created = await response.Content.ReadFromJsonAsync<StudentDto>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Should().NotBeNull();
        created!.Name.Should().Be("John");
        created.Email.Should().Be("john@test.com");
    }

    [Fact]
    public async Task GetById_ShouldReturn200_AfterCreation()
    {
        // Arrange — create first
        var dto = new CreateStudentDto { Name = "Jane", Email = "jane@test.com", Status = StudentStatus.Active };
        var createResponse = await _client.PostAsJsonAsync("/api/students", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<StudentDto>();

        // Act — fetch by id from same client (same DB)
        var response = await _client.GetAsync($"/api/students/{created!.StudentId}");
        var student = await response.Content.ReadFromJsonAsync<StudentDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        student!.Name.Should().Be("Jane");
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenStudentExists()
    {
        // Arrange — create first
        var dto = new CreateStudentDto { Name = "ToDelete", Email = "delete@test.com", Status = StudentStatus.Active };
        var createResponse = await _client.PostAsJsonAsync("/api/students", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<StudentDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/students/{created!.StudentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}