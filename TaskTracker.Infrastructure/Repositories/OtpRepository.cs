using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class OtpRepository : IOtpRepository
{
    private readonly AppDbContext _dbContext;

    public OtpRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OtpEntry> AddAsync(OtpEntry otpEntry, CancellationToken cancellationToken = default)
    {
        _dbContext.OtpEntries.Add(otpEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return otpEntry;
    }

    public async Task<OtpEntry> UpdateAsync(OtpEntry otpEntry, CancellationToken cancellationToken = default)
    {
        _dbContext.OtpEntries.Update(otpEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return otpEntry;
    }

    public async Task<OtpEntry?> GetLatestActiveByUserIdAndPurposeAsync(
        string userId,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OtpEntries
            .Where(o => o.UserId == userId
                     && o.Purpose == purpose
                     && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<OtpEntry>> GetByUserIdAndPurposeAsync(
        string userId,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OtpEntries
            .Where(o => o.UserId == userId && o.Purpose == purpose)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid otpId, CancellationToken cancellationToken = default)
    {
        var otp = await _dbContext.OtpEntries.FindAsync(new object?[] { otpId }, cancellationToken: cancellationToken);
        if (otp is not null)
        {
            _dbContext.OtpEntries.Remove(otp);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
