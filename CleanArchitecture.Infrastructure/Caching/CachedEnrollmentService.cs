using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Caching;

public class CachedEnrollmentService(
    IEnrollmentService inner,
    ICacheService cache,
    ILogger<CachedEnrollmentService> logger) : IEnrollmentService
{
    private readonly IEnrollmentService _inner = inner;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<CachedEnrollmentService> _logger = logger;

    private const string AllKey = "enrollments:all";
    private static string StudentKey(int studentId) => $"enrollments:student:{studentId}";
    private static string CourseKey(int courseId) => $"enrollments:course:{courseId}";

    public async Task<IEnumerable<EnrollmentDto>> GetAllAsync()
    {
        var cached = await _cache.GetAsync<IEnumerable<EnrollmentDto>>(AllKey);

        if (cached != null)
        {
            _logger.LogInformation("CACHE HIT: {Key}", AllKey);
            return cached;
        }

        _logger.LogInformation("CACHE MISS: {Key}", AllKey);
        var result = await _inner.GetAllAsync();
        await _cache.SetAsync(AllKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task<IEnumerable<EnrollmentDto>> GetByStudentIdAsync(int studentId)
    {
        var key = StudentKey(studentId);
        var cached = await _cache.GetAsync<IEnumerable<EnrollmentDto>>(key);

        if (cached != null)
        {
            _logger.LogInformation("CACHE HIT: {Key}", key);
            return cached;
        }

        _logger.LogInformation("CACHE MISS: {Key}", key);
        var result = await _inner.GetByStudentIdAsync(studentId);
        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task<IEnumerable<EnrollmentDto>> GetByCourseIdAsync(int courseId)
    {
        var key = CourseKey(courseId);
        var cached = await _cache.GetAsync<IEnumerable<EnrollmentDto>>(key);

        if (cached != null)
        {
            _logger.LogInformation("CACHE HIT: {Key}", key);
            return cached;
        }

        _logger.LogInformation("CACHE MISS: {Key}", key);
        var result = await _inner.GetByCourseIdAsync(courseId);
        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task<EnrollmentDto> CreateAsync(CreateEnrollmentDto dto)
    {
        var result = await _inner.CreateAsync(dto);

        _logger.LogInformation(
            "CACHE INVALIDATE: all enrollment keys (reason: enrollment created, student: {StudentId}, course: {CourseId})",
            dto.StudentId, dto.CourseId);

        await InvalidateAllAsync(dto.StudentId, dto.CourseId);

        return result;
    }

    public async Task<bool> DeleteAsync(int studentId, int courseId)
    {
        var result = await _inner.DeleteAsync(studentId, courseId);

        if (result)
        {
            _logger.LogInformation(
                "CACHE INVALIDATE: all enrollment keys (reason: enrollment deleted, student: {StudentId}, course: {CourseId})",
                studentId, courseId);

            await InvalidateAllAsync(studentId, courseId);
        }
        else
        {
            _logger.LogWarning(
                "DELETE skipped cache invalidation: enrollment not found (student: {StudentId}, course: {CourseId})",
                studentId, courseId);
        }

        return result;
    }

    private async Task InvalidateAllAsync(int studentId, int courseId)
    {
        await Task.WhenAll(
            _cache.RemoveAsync(AllKey),
            _cache.RemoveAsync(StudentKey(studentId)),
            _cache.RemoveAsync(CourseKey(courseId))
        );
    }
}