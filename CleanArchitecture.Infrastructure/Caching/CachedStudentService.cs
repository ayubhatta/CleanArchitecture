using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Caching;

public class CachedStudentService(
    IStudentService inner,
    ICacheService cache,
    ILogger<CachedStudentService> logger) : IStudentService
{
    private readonly IStudentService _inner = inner;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<CachedStudentService> _logger = logger;

    private const string AllKey = "students:all";
    private static string ByIdKey(int id) => $"students:{id}";

    public async Task<IEnumerable<StudentDto>> GetAllAsync()
    {
        var cached = await _cache.GetAsync<IEnumerable<StudentDto>>(AllKey);

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

    public async Task<StudentDto?> GetByIdAsync(int id)
    {
        var key = ByIdKey(id);
        var cached = await _cache.GetAsync<StudentDto>(key);

        if (cached != null)
        {
            _logger.LogInformation("CACHE HIT: {Key}", key);
            return cached;
        }

        _logger.LogInformation("CACHE MISS: {Key}", key);
        var result = await _inner.GetByIdAsync(id);

        if (result is not null)
            await _cache.SetAsync(key, result, TimeSpan.FromMinutes(10));

        return result;
    }

    public async Task<StudentDto> CreateAsync(CreateStudentDto dto)
    {
        var result = await _inner.CreateAsync(dto);

        _logger.LogInformation(
            "CACHE INVALIDATE: students:all (reason: student created, id: {Id})",
            result.StudentId);

        await _cache.RemoveAsync(AllKey);

        return result;
    }

    public async Task<StudentDto?> UpdateAsync(int id, UpdateStudentDto dto)
    {
        var result = await _inner.UpdateAsync(id, dto);

        if (result is not null)
        {
            _logger.LogInformation(
                "CACHE INVALIDATE: students:all + students:{Id} (reason: student updated)",
                id);

            await Task.WhenAll(
                _cache.RemoveAsync(AllKey),
                _cache.RemoveAsync(ByIdKey(id))
            );
        }
        else
        {
            _logger.LogWarning("UPDATE skipped cache invalidation: student id {Id} not found", id);
        }

        return result;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var result = await _inner.DeleteAsync(id);

        if (result)
        {
            _logger.LogInformation(
                "CACHE INVALIDATE: students:all + students:{Id} (reason: student deleted)",
                id);

            await Task.WhenAll(
                _cache.RemoveAsync(AllKey),
                _cache.RemoveAsync(ByIdKey(id))
            );
        }
        else
        {
            _logger.LogWarning("DELETE skipped cache invalidation: student id {Id} not found", id);
        }

        return result;
    }
}