using SportsLeague.Domain.Entities;

namespace SportsLeague.Domain.Interfaces.Services;

public interface IMatchEventService
{
    // MatchResult 
    Task<MatchResult> RegisterResultAsync(int matchId, MatchResult result);
    Task<MatchResult?> GetResultByMatchAsync(int matchId);

    // Goals 
    Task<Goal> RegisterGoalAsync(int matchId, Goal goal);
    Task<IEnumerable<Goal>> GetGoalsByMatchAsync(int matchId);
    Task DeleteGoalAsync(int goalId);

    // Cards 
    Task<Card> RegisterCardAsync(int matchId, Card card);
    Task<IEnumerable<Card>> GetCardsByMatchAsync(int matchId);
    Task DeleteCardAsync(int cardId);

    //MatchLineup
    Task<MatchLineup> RegisterLineupAsync(int matchId, MatchLineup lineup);
    Task<IEnumerable<MatchLineup>> GetLineupsByMatchAsync(int matchId);
    Task<IEnumerable<MatchLineup>> GetLineupsByMatchAndTeamAsync(int matchId, int teamId);
    Task DeleteLineupAsync(int matchId, int lineupId);
}