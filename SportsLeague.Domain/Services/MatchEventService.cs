using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Helpers;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.Domain.Services;

public class MatchEventService : IMatchEventService
{
    private readonly IMatchRepository _matchRepository;
    private readonly IMatchResultRepository _matchResultRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IMatchLineupRepository _lineupRepository; // Nuevo 
    private readonly IPlayerRepository _playerRepository;   // Nuevo
    private readonly MatchValidationHelper _validationHelper;
    private readonly ILogger<MatchEventService> _logger;

    public MatchEventService(
        IMatchRepository matchRepository,
        IMatchResultRepository matchResultRepository,
        IGoalRepository goalRepository,
        ICardRepository cardRepository,
        IMatchLineupRepository lineupRepository, // Nuevo
        IPlayerRepository playerRepository,   //Nuevo
        MatchValidationHelper validationHelper,
        ILogger<MatchEventService> logger)
    {
        _matchRepository = matchRepository;
        _matchResultRepository = matchResultRepository;
        _goalRepository = goalRepository;
        _cardRepository = cardRepository;
        _lineupRepository = lineupRepository;   // Nuevo
        _playerRepository = playerRepository;   //Nuevo
        _validationHelper = validationHelper;
        _logger = logger;
    }

    // ═══ MatchResult ═══ 

    public async Task<MatchResult> RegisterResultAsync(
        int matchId, MatchResult result)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        if (match.Status != MatchStatus.Finished)
            throw new InvalidOperationException("Solo se puede registrar resultado en partidos con estado Finished");

        var existingResult = await _matchResultRepository
            .GetByMatchIdAsync(matchId);
        if (existingResult != null)
            throw new InvalidOperationException("Este partido ya tiene un resultado registrado");

        if (result.HomeGoals < 0 || result.AwayGoals < 0)
            throw new InvalidOperationException("Los goles no pueden ser negativos");

        result.MatchId = matchId;

        _logger.LogInformation("Registering result for match {MatchId}: {Home}-{Away}",
            matchId, result.HomeGoals, result.AwayGoals);
        return await _matchResultRepository.CreateAsync(result);
    }

    public async Task<MatchResult?> GetResultByMatchAsync(int matchId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");
        return await _matchResultRepository.GetByMatchIdAsync(matchId);
    }

    // ═══ Goals ═══ 

    public async Task<Goal> RegisterGoalAsync(int matchId, Goal goal)
    {
        var match = await _validationHelper.ValidateMatchForEventAsync(matchId);
        await _validationHelper.ValidatePlayerInMatchAsync(goal.PlayerId, match);
        MatchValidationHelper.ValidateMinute(goal.Minute);

        goal.MatchId = matchId;

        _logger.LogInformation("Registering goal: Match {MatchId}, Player {PlayerId}, Minute {Minute}",
            matchId, goal.PlayerId, goal.Minute);
        return await _goalRepository.CreateAsync(goal);
    }

    public async Task<IEnumerable<Goal>> GetGoalsByMatchAsync(int matchId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        return await _goalRepository.GetByMatchWithDetailsAsync(matchId);
    }

    public async Task DeleteGoalAsync(int goalId)
    {
        var exists = await _goalRepository.ExistsAsync(goalId);
        if (!exists)
            throw new KeyNotFoundException( $"No se encontró el gol con ID {goalId}");
        await _goalRepository.DeleteAsync(goalId);
    }

    // ═══ Cards ═══ 

    public async Task<Card> RegisterCardAsync(int matchId, Card card)
    {
        var match = await _validationHelper.ValidateMatchForEventAsync(matchId);
        await _validationHelper.ValidatePlayerInMatchAsync(card.PlayerId, match);
        MatchValidationHelper.ValidateMinute(card.Minute);

        card.MatchId = matchId;
        _logger.LogInformation("Registering {CardType} card: Match {MatchId}, Player {PlayerId}",
            card.Type, matchId, card.PlayerId);
        return await _cardRepository.CreateAsync(card);
    }
    public async Task<IEnumerable<Card>> GetCardsByMatchAsync(int matchId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
            throw new KeyNotFoundException( $"No se encontró el partido con ID {matchId}");

        return await _cardRepository.GetByMatchWithDetailsAsync(matchId);
    }

    public async Task DeleteCardAsync(int cardId)
    {
        var exists = await _cardRepository.ExistsAsync(cardId);
        if (!exists)
            throw new KeyNotFoundException( $"No se encontró la tarjeta con ID {cardId}");
        await _cardRepository.DeleteAsync(cardId);
    }

    // ═══ Match Lineups ═══ 
    public async Task<MatchLineup> RegisterLineupAsync(int matchId, MatchLineup lineup)
    {
        // El partido existe y esta en Scheduled
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        if (match.Status != MatchStatus.Scheduled)
            throw new InvalidOperationException("Solo se pueden registrar alineaciones en partidos con estado Scheduled");

        // El jugador existir
        var player = await _playerRepository.GetByIdAsync(lineup.PlayerId);
        if (player == null)
            throw new KeyNotFoundException($"No se encontró el jugador con ID {lineup.PlayerId}");

        // El jugador pertenece a HomeTeam o AwayTeam en el Match
        if (player.TeamId != match.HomeTeamId && player.TeamId != match.AwayTeamId)
            throw new InvalidOperationException("El jugador no pertenece a ninguno de los equipos inscritos en este partido");

        // El jugador no puede estar registrado dos veces en una alineación para el mismo partido
        var isDuplicated = await _lineupRepository.ExistsByMatchAndPlayerAsync(matchId, lineup.PlayerId);
        if (isDuplicated)
            throw new InvalidOperationException("El jugador ya está registrado en la alineación de este partido");

        // Máximo 11 titulares por equipo por partido cuando IsStarter is true, es titular; y cuando hay mas de 11, se lanza error 
        if (lineup.IsStarter)
        {
            var currentLineups = await _lineupRepository.GetByMatchAndTeamAsync(matchId, player.TeamId);
            int startersCount = currentLineups.Count(ml => ml.IsStarter);

            if (startersCount > 11)
                throw new InvalidOperationException("El equipo ya tiene 11 titulares registrados en este partido");
        }

        lineup.MatchId = matchId;

        _logger.LogInformation("Registering lineup: Match {MatchId}, Player {PlayerId}, Starter: {IsStarter}",
            matchId, lineup.PlayerId, lineup.IsStarter);

        return await _lineupRepository.CreateAsync(lineup);
    }

    public async Task<IEnumerable<MatchLineup>> GetLineupsByMatchAsync(int matchId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        return await _lineupRepository.GetByMatchAsync(matchId);
    }

    public async Task<IEnumerable<MatchLineup>> GetLineupsByMatchAndTeamAsync(int matchId, int teamId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        return await _lineupRepository.GetByMatchAndTeamAsync(matchId, teamId);
    }

    public async Task DeleteLineupAsync(int matchId, int lineupId)
    {
        var lineup = await _lineupRepository.GetByIdAsync(lineupId);
        if (lineup == null || lineup.MatchId != matchId)
            throw new KeyNotFoundException($"No se encontró la alineación con ID {lineupId} para este partido");

        _logger.LogInformation("Deleting lineup registry with ID: {LineupId} from match {MatchId}", lineupId, matchId);
        await _lineupRepository.DeleteAsync(lineupId);
    }
}