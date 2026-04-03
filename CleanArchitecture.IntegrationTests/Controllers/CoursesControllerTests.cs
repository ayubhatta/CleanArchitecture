using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Enums;
using FluentAssertions;

namespace CleanArchitecture.IntegrationTests.Controllers;

public class CoursesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/courses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenCourseDoesNotExist()
    {
        var response = await _client.GetAsync("/api/courses/9999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WithCreatedCourse()
    {
        var dto = new CreateCourseDto { CourseName = "C# Basics", Credits = 3, Level = CourseLevel.Beginner };

        var response = await _client.PostAsJsonAsync("/api/courses", dto);
        var created = await response.Content.ReadFromJsonAsync<CourseDto>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        created!.CourseName.Should().Be("C# Basics");
        created.Credits.Should().Be(3);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WithUpdatedCourse()
    {
        var createDto = new CreateCourseDto { CourseName = "Old Name", Credits = 3, Level = CourseLevel.Beginner };
        var createResponse = await _client.PostAsJsonAsync("/api/courses", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<CourseDto>();

        var updateDto = new UpdateCourseDto { CourseName = "New Name", Credits = 5, Level = CourseLevel.Advanced };

        var response = await _client.PutAsJsonAsync($"/api/courses/{created!.CourseId}", updateDto);
        var updated = await response.Content.ReadFromJsonAsync<CourseDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.CourseName.Should().Be("New Name");
        updated.Credits.Should().Be(5);
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenCourseExists()
    {
        var dto = new CreateCourseDto { CourseName = "ToDelete", Credits = 1, Level = CourseLevel.Beginner };
        var createResponse = await _client.PostAsJsonAsync("/api/courses", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<CourseDto>();

        var response = await _client.DeleteAsync($"/api/courses/{created!.CourseId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}