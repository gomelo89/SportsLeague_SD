namespace SportsLeague.Domain.Entities;

<<<<<<< HEAD
public class Team : AuditBase
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Stadium { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public DateTime FoundedDate { get; set; }
    // Navigation Properties 
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<TournamentTeam> TournamentTeams { get; set; } = new List<TournamentTeam>();

}
=======
    public class Team : AuditBase

    {

        public string Name { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Stadium { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }

        public DateTime FoundedDate { get; set; }

    }

>>>>>>> 2eba5c4a76d5a55695781a3f6395d315c88a5c96
