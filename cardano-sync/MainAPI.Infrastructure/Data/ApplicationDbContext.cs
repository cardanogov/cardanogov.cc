using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace MainAPI.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // SharedLibrary Models - sử dụng snake_case table names
        public DbSet<MDPoolInformation> pool_information { get; set; }
        public DbSet<MDPoolList> pool_list { get; set; }
        public DbSet<MDVoteList> vote_list { get; set; }
        public DbSet<MDProposalsList> proposals_list { get; set; }
        public DbSet<MDPoolDelegators> pool_delegators { get; set; }
        public DbSet<MDPoolMetadata> pool_metadata { get; set; }
        public DbSet<MDPoolUpdates> pool_updates { get; set; }
        public DbSet<MDPoolStakeSnapshot> pool_stake_snapshot { get; set; }
        public DbSet<MDPoolsVotingPowerHistory> pools_voting_power_history { get; set; }
        public DbSet<MDDreps> dreps { get; set; }
        public DbSet<MDDrepsList> dreps_list { get; set; }
        public DbSet<MDDrepsInfo> dreps_info { get; set; }
        public DbSet<MDDrepsMetadata> dreps_metadata { get; set; }
        public DbSet<MDDrepsUpdates> dreps_updates { get; set; }
        public DbSet<MDDrepsVotes> dreps_votes { get; set; }
        public DbSet<MDDrepsDelegators> dreps_delegators { get; set; }
        public DbSet<MDDrepsVotingPowerHistory> dreps_voting_power_history { get; set; }
        public DbSet<MDDrepsEpochSummary> dreps_epoch_summary { get; set; }
        public DbSet<MDCommitteeInformation> committee_information { get; set; }
        public DbSet<MDCommitteeVotes> committee_votes { get; set; }
        public DbSet<MDEpoch> epoch { get; set; }
        public DbSet<MDEpochs> epochs { get; set; }
        public DbSet<MDEpochProtocolParameters> epoch_protocol_parameters { get; set; }
        public DbSet<MDProposalVotes> proposal_votes { get; set; }
        public DbSet<MDProposalVotingSummary> proposal_voting_summary { get; set; }
        public DbSet<MDVotersProposalList> voters_proposal_list { get; set; }
        public DbSet<MDTotals> totals { get; set; }
        public DbSet<MDTreasuryWithdrawals> treasury_withdrawals { get; set; }
        public DbSet<MDUSD> usd { get; set; }
        public DbSet<MDUtxoInfo> utxo_info { get; set; }
        public DbSet<MDQueryChainTip> query_chain_tip { get; set; }
        public DbSet<MDAccountList> account_list { get; set; }
        public DbSet<MDAccountInformation> account_information { get; set; }
        public DbSet<MDAccountUpdates> account_updates { get; set; }
        public DbSet<ApiKey> api_key { get; set; }
        public DbSet<GeneratedImage> images { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure SharedLibrary entities as keyless (read-only views)
            ConfigureSharedLibraryEntities(modelBuilder);

            // Configure MainAPI entities
            ConfigureMainAPIEntities(modelBuilder);
        }

        private void ConfigureSharedLibraryEntities(ModelBuilder modelBuilder)
        {
            // Configure all SharedLibrary entities as keyless (read-only)
            modelBuilder.Entity<MDPoolInformation>().HasNoKey().ToTable("md_pool_information");
            modelBuilder.Entity<MDPoolList>().HasNoKey().ToTable("md_pool_list");
            modelBuilder.Entity<MDVoteList>().HasNoKey().ToTable("md_vote_list");
            modelBuilder.Entity<MDProposalsList>().HasNoKey().ToTable("md_proposals_list");
            modelBuilder.Entity<MDPoolDelegators>().HasNoKey().ToTable("md_pool_delegators");
            modelBuilder.Entity<MDPoolMetadata>().HasNoKey().ToTable("md_pool_metadata");
            modelBuilder.Entity<MDPoolUpdates>().HasNoKey().ToTable("md_pool_updates");
            modelBuilder.Entity<MDPoolStakeSnapshot>().HasNoKey().ToTable("md_pool_stake_snapshot");
            modelBuilder.Entity<MDPoolsVotingPowerHistory>().HasNoKey().ToTable("md_pools_voting_power_history");
            modelBuilder.Entity<MDDreps>().HasNoKey().ToTable("md_dreps");
            modelBuilder.Entity<MDDrepsList>().HasNoKey().ToTable("md_dreps_list");
            modelBuilder.Entity<MDDrepsInfo>().HasNoKey().ToTable("md_dreps_info");
            modelBuilder.Entity<MDDrepsMetadata>().HasNoKey().ToTable("md_dreps_metadata");
            modelBuilder.Entity<MDDrepsUpdates>().HasNoKey().ToTable("md_dreps_updates");
            modelBuilder.Entity<MDDrepsVotes>().HasNoKey().ToTable("md_dreps_votes");
            modelBuilder.Entity<MDDrepsDelegators>().HasNoKey().ToTable("md_dreps_delegators");
            modelBuilder.Entity<MDDrepsVotingPowerHistory>().HasNoKey().ToTable("md_dreps_voting_power_history");
            modelBuilder.Entity<MDDrepsEpochSummary>().HasNoKey().ToTable("md_dreps_epoch_summary");
            modelBuilder.Entity<MDCommitteeInformation>().HasNoKey().ToTable("md_committee_information");
            modelBuilder.Entity<MDCommitteeVotes>().HasNoKey().ToTable("md_committee_votes");
            modelBuilder.Entity<MDEpoch>().HasNoKey().ToTable("md_epoch");
            modelBuilder.Entity<MDEpochs>().HasNoKey().ToTable("md_epochs");
            modelBuilder.Entity<MDEpochProtocolParameters>().HasNoKey().ToTable("md_epoch_protocol_parameters");
            modelBuilder.Entity<MDProposalVotes>().HasNoKey().ToTable("md_proposal_votes");
            modelBuilder.Entity<MDProposalVotingSummary>().HasNoKey().ToTable("md_proposal_voting_summary");
            modelBuilder.Entity<MDVotersProposalList>().HasNoKey().ToTable("md_voters_proposal_list");
            modelBuilder.Entity<MDTotals>().HasNoKey().ToTable("md_totals");
            modelBuilder.Entity<MDTreasuryWithdrawals>().HasNoKey().ToTable("md_treasury_withdrawals");
            modelBuilder.Entity<MDUSD>().HasNoKey().ToTable("md_usd");
            modelBuilder.Entity<MDUtxoInfo>().HasNoKey().ToTable("md_utxo_info");
            modelBuilder.Entity<MDQueryChainTip>().HasNoKey().ToTable("md_query_chain_tip");
            modelBuilder.Entity<MDAccountList>().HasNoKey().ToTable("md_account_list");
            modelBuilder.Entity<MDAccountInformation>().HasNoKey().ToTable("md_account_information");
            modelBuilder.Entity<MDAccountUpdates>().HasNoKey().ToTable("md_account_updates");
            modelBuilder.Entity<GeneratedImage>().ToTable("generated_image");
            modelBuilder.Entity<ApiKey>().ToTable("api_key");

            // Ignore navigation properties for keyless entities
            IgnoreNavigationProperties(modelBuilder);
        }

        private void ConfigureMainAPIEntities(ModelBuilder modelBuilder)
        {
            // Configure performance indexes for MainAPI tables
            ConfigureMainAPIPerformanceIndexes(modelBuilder);
        }

        private void IgnoreNavigationProperties(ModelBuilder modelBuilder)
        {
            // Ignore all navigation properties for keyless entities
            // This prevents EF Core from trying to create relationships
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(e => e.ClrType.Namespace == "SharedLibrary.Models");

            foreach (var entityType in entityTypes)
            {
                var navigations = entityType.GetNavigations().ToList();
                foreach (var navigation in navigations)
                {
                    modelBuilder.Entity(entityType.ClrType).Ignore(navigation.Name);
                }
            }
        }

        /// <summary>
        /// Configure performance indexes specifically for MainAPI operations
        /// These complement the SharedLibrary indexes for optimal API performance
        /// </summary>
        private void ConfigureMainAPIPerformanceIndexes(ModelBuilder modelBuilder)
        {
            // Note: Additional indexes for MainAPI-specific query patterns
            // These should be created via database migrations

            /*
            MAINAPI-SPECIFIC PERFORMANCE INDEXES:

            -- Focus on Cardano-specific data optimization only
            -- API Key and Generated Image tables are excluded from indexing optimization

            -- Partial indexes for frequently accessed data
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_active_only ON md_dreps_info(drep_id, amount) WHERE active = true;
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pools_active_only ON md_pool_list(pool_id_bech32, active_stake) WHERE pool_status = 'active';
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposals_live_only ON md_proposals_list(proposal_id, proposed_epoch DESC) WHERE expired_epoch IS NULL AND enacted_epoch IS NULL AND proposed_epoch >= 507;
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_votes_recent_only ON md_vote_list(voter_id, block_time DESC) WHERE block_time > EXTRACT(EPOCH FROM NOW() - INTERVAL '30 days');

            -- Covering indexes for read-heavy queries (include commonly selected columns)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_list_covering ON md_dreps_info(drep_id) INCLUDE (active, expires_epoch_no, amount);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pools_list_covering ON md_pool_list(pool_id_bech32) INCLUDE (pool_status, active_stake, ticker);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposals_covering ON md_proposals_list(proposal_id) INCLUDE (proposed_epoch, expired_epoch, enacted_epoch, block_time);

            -- Indexes for pagination and sorting (ORDER BY + LIMIT queries)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_pagination ON md_dreps_voting_power_history(epoch_no DESC, amount DESC, drep_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pools_pagination ON md_pool_list(active_stake DESC, pool_id_bech32);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposals_pagination ON md_proposals_list(block_time DESC, proposal_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_votes_pagination ON md_vote_list(block_time DESC, voter_id, proposal_id);

            -- Indexes for aggregation queries (GROUP BY, COUNT, SUM)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_aggregation ON md_dreps_delegators(drep_id) INCLUDE (amount);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pools_aggregation ON md_pool_delegators(pool_id_bech32) INCLUDE (amount);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_votes_aggregation ON md_vote_list(voter_id, voter_role) INCLUDE (proposal_id);

            -- Indexes for search functionality (LIKE, ILIKE queries)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_search_trgm ON md_dreps_metadata USING gin(drep_id gin_trgm_ops);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pools_search_trgm ON md_pool_list USING gin(ticker gin_trgm_ops);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pools_search_bech32_trgm ON md_pool_list USING gin(pool_id_bech32 gin_trgm_ops);

            -- Indexes for time-range queries (dashboard analytics)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_epochs_time_range ON md_epochs(start_time, end_time) INCLUDE (no, delegator, account);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_voting_power_time_range ON md_dreps_voting_power_history(epoch_no) INCLUDE (drep_id, amount);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_account_updates_time_range ON md_account_updates(epoch_no, block_time) INCLUDE (stake_address, action_type);
            */
        }
    }
}