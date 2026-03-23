using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Gamification;

namespace SSPMS.Application.Interfaces;

public interface IGamificationService
{
    Task<IEnumerable<LeaderboardEntry>> GetClassLeaderboardAsync(Guid classId, string period);
    Task<IEnumerable<LeaderboardEntry>> GetGlobalLeaderboardAsync(string period);
    Task<IEnumerable<BadgeDto>> GetAllBadgesAsync();
    Task<IEnumerable<EmployeeBadgeDto>> GetEmployeeBadgesAsync(Guid employeeId);
    Task<XPSummaryDto> GetXPSummaryAsync(Guid employeeId);
    Task<DashboardStatsDto> GetEmployeeDashboardAsync(Guid employeeId);
    Task ProcessBadgesForSubmissionAsync(Guid submissionId);
    Task UpdateStreakAsync(Guid employeeId);
    decimal GetMultiplierForRank(int rank);
}
