using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Repositories;

public class EnrollmentRepository(AppDbContext context)
    : RepositoryBase<StudentCourse>(context), IEnrollmentRepository
{
    public new async Task<List<StudentCourse>> FindAllAsync(bool trackChanges = false) =>
        await Context.Set<StudentCourse>()
            .Include(sc => sc.Student)
            .Include(sc => sc.Course)
            .AsNoTracking()
            .ToListAsync();

    public async Task<StudentCourse?> GetByIdAsync(int studentId, int courseId) =>
        await FindByCondition(sc => sc.StudentId == studentId && sc.CourseId == courseId)
            .Include(sc => sc.Student)
            .Include(sc => sc.Course)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<StudentCourse>> GetByStudentIdAsync(int studentId) =>
        await FindByCondition(sc => sc.StudentId == studentId)
            .Include(sc => sc.Student)
            .Include(sc => sc.Course)
            .ToListAsync();

    public async Task<IEnumerable<StudentCourse>> GetByCourseIdAsync(int courseId) =>
        await FindByCondition(sc => sc.CourseId == courseId)
            .Include(sc => sc.Student)
            .Include(sc => sc.Course)
            .ToListAsync();
}