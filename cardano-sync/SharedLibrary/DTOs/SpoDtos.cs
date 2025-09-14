namespace SharedLibrary.DTOs
{
    public class SpoStatsDto
    {
        public double? total_staked { get; set; }
        public int? active_pools { get; set; }
        public int? total_stake_addresses { get; set; }
        public SpoVotingPowerDto? voting_power { get; set; }
    }

    public class SpoVotingPowerDto
    {
        public int? active { get; set; }
        public int? inactive { get; set; }
    }
}