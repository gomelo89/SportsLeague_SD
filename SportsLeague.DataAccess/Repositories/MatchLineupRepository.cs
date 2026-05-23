using Microsoft.EntityFrameworkCore;
using SportsLeague.DataAccess.Context;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Repositories;

namespace SportsLeague.DataAccess.Repositories
{

    // Usamos el generic reposity para heredar el CRUD para luego agregar los metodos espeficos de MatchLineup
    public class MatchLineupRepository : GenericRepository<MatchLineup>, IMatchLineupRepository
    {
        private readonly LeagueDbContext _context;

        //este constructor inyecta el contexto de la BD
        public MatchLineupRepository(LeagueDbContext context) : base(context)
        {
            _context = context;
        }
        // Metodos especificos para MatchLineup
        public async Task<IEnumerable<MatchLineup>> GetByMatchAsync(int matchId)
        {
            return await _context.MatchLineups
                .Include(ml => ml.Player)
                    .ThenInclude(p => p.Team)
                .Where(ml => ml.MatchId == matchId)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<MatchLineup>> GetByMatchAndTeamAsync(int matchId, int teamId)
        {
            return await _context.MatchLineups
                .Include(ml => ml.Player)
                    .ThenInclude(p => p.Team)
                .Where(ml => ml.MatchId == matchId && ml.Player.TeamId == teamId)
                .ToListAsync();
        }
        
        public async Task<bool> ExistsByMatchAndPlayerAsync(int matchId, int playerId)
        {
            return await _context.MatchLineups
                .AnyAsync(ml => ml.MatchId == matchId && ml.PlayerId == playerId);
        }
    }
}