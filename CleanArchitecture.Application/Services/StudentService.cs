using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Events;
using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Services;

public class StudentService(
    IStudentRepository repository,
    IMessagePublisher publisher) : IStudentService
{
    public async Task<IEnumerable<StudentDto>> GetAllAsync()
    {
        var students = await repository.FindAllAsync();
        return students.Select(s => new StudentDto
        {
            StudentId = s.StudentId,
            Name = s.Name,
            Email = s.Email,
            Status = s.Status
        });
    }

    public async Task<StudentDto?> GetByIdAsync(int id)
    {
        var student = await repository.GetByIdAsync(id);
        if (student is null) return null;

        return new StudentDto
        {
            StudentId = student.StudentId,
            Name = student.Name,
            Email = student.Email,
            Status = student.Status
        };
    }

    public async Task<StudentDto> CreateAsync(CreateStudentDto dto)
    {
        var student = new Student
        {
            Name = dto.Name,
            Email = dto.Email,
            Status = dto.Status
        };

        repository.Create(student);
        await repository.SaveChangesAsync();

        await publisher.PublishAsync("student.registered", new StudentRegisteredEvent
        {
            StudentId = student.StudentId,
            StudentName = student.Name,
            StudentEmail = student.Email
        });

        return new StudentDto
        {
            StudentId = student.StudentId,
            Name = student.Name,
            Email = student.Email,
            Status = student.Status
        };
    }

    public async Task<StudentDto?> UpdateAsync(int id, UpdateStudentDto dto)
    {
        var student = await repository.GetByIdAsync(id);
        if (student is null) return null;

        student.Name = dto.Name;
        student.Email = dto.Email;
        student.Status = dto.Status;

        repository.Update(student);
        await repository.SaveChangesAsync();

        await publisher.PublishAsync("audit.log", new AuditEvent
        {
            Entity = "Student",
            Action = "Updated",
            EntityId = id
        });

        return new StudentDto
        {
            StudentId = student.StudentId,
            Name = student.Name,
            Email = student.Email,
            Status = student.Status
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var student = await repository.GetByIdAsync(id);
        if (student is null) return false;

        repository.Delete(student);
        await repository.SaveChangesAsync();

        await publisher.PublishAsync("audit.log", new AuditEvent
        {
            Entity = "Student",
            Action = "Deleted",
            EntityId = id
        });

        return true;
    }
}