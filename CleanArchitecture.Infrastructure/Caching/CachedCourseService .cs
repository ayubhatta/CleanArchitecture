using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Caching;

public class CachedCourseService(
    ICourseService inner,
    ICacheService cache,
    ILogger<CachedCourseService> logger) : ICourseService
{
    private readonly ICourseService _inner = inner;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<CachedCourseService> _logger = logger;

    const string cacheKey = "courses:all";

    public async Task<IEnumerable<CourseDto>> GetAllAsync()
    {
        var cached = await _cache.GetAsync<IEnumerable<CourseDto>>(cacheKey);

        if (cached != null)
        {
            _logger.LogInformation("CACHE HIT: {Key}", cacheKey);
            return cached;
        }

        _logger.LogInformation("CACHE MISS: {Key}", cacheKey);

        var result = await _inner.GetAllAsync();

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

        return result;
    }

    public async Task<CourseDto> CreateAsync(CreateCourseDto dto)
    {
        var result = await _inner.CreateAsync(dto);

        _logger.LogInformation("CACHE INVALIDATE: courses:all (reason: course created, id: {Id})", result.CourseId);
        await _cache.RemoveAsync("courses:all");

        return result;
    }

    public async Task<CourseDto?> UpdateAsync(int id, UpdateCourseDto dto)
    {
        var result = await _inner.UpdateAsync(id, dto);

        if (result is not null)
        {
            _logger.LogInformation("CACHE INVALIDATE: courses:all (reason: course updated, id: {Id})", id);
            await _cache.RemoveAsync("courses:all");
        }
        else
        {
            _logger.LogWarning("UPDATE skipped cache invalidation: course id {Id} not found", id);
        }

        return result;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var result = await _inner.DeleteAsync(id);

        if (result)
        {
            _logger.LogInformation("CACHE INVALIDATE: courses:all (reason: course deleted, id: {Id})", id);
            await _cache.RemoveAsync("courses:all");
        }
        else
        {
            _logger.LogWarning("DELETE skipped cache invalidation: course id {Id} not found", id);
        }

        return result;
    }

    public Task<CourseDto?> GetByIdAsync(int id)
        => _inner.GetByIdAsync(id);
}