using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Events;
using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Services;

public class CourseService(
    ICourseRepository repository,
    IEnrollmentRepository enrollmentRepository,
    IMessagePublisher publisher) : ICourseService
{
    public async Task<IEnumerable<CourseDto>> GetAllAsync()
    {
        var courses = await repository.FindAllAsync();
        return courses.Select(c => new CourseDto
        {
            CourseId = c.CourseId,
            CourseName = c.CourseName,
            Credits = c.Credits,
            Level = c.Level
        });
    }

    public async Task<CourseDto?> GetByIdAsync(int id)
    {
        var course = await repository.GetByIdAsync(id);
        if (course is null) return null;

        return new CourseDto
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            Credits = course.Credits,
            Level = course.Level
        };
    }

    public async Task<CourseDto> CreateAsync(CreateCourseDto dto)
    {
        var course = new Course
        {
            CourseName = dto.CourseName,
            Credits = dto.Credits,
            Level = dto.Level
        };

        repository.Create(course);
        await repository.SaveChangesAsync();

        await publisher.PublishAsync("audit.log", new AuditEvent
        {
            Entity = "Course",
            Action = "Created",
            EntityId = course.CourseId
        });

        return new CourseDto
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            Credits = course.Credits,
            Level = course.Level
        };
    }

    public async Task<CourseDto?> UpdateAsync(int id, UpdateCourseDto dto)
    {
        var course = await repository.GetByIdAsync(id);
        if (course is null) return null;

        course.CourseName = dto.CourseName;
        course.Credits = dto.Credits;
        course.Level = dto.Level;

        repository.Update(course);
        await repository.SaveChangesAsync();

        var enrollments = await enrollmentRepository.GetByCourseIdAsync(id);
        var students = enrollments.Select(e => new EnrolledStudentInfo
        {
            StudentName = e.Student.Name,
            StudentEmail = e.Student.Email
        }).ToList();

        await publisher.PublishAsync("course.updated", new CourseUpdatedEvent
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            EnrolledStudents = students
        });

        return new CourseDto
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            Credits = course.Credits,
            Level = course.Level
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var course = await repository.GetByIdAsync(id);
        if (course is null) return false;

        repository.Delete(course);
        await repository.SaveChangesAsync();

        await publisher.PublishAsync("audit.log", new AuditEvent
        {
            Entity = "Course",
            Action = "Deleted",
            EntityId = id
        });

        return true;
    }
}