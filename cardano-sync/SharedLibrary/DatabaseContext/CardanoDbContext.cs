using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace SharedLibrary.DatabaseContext
{
    public class CardanoDbContext : DbContext
    {
        public CardanoDbContext(DbContextOptions<CardanoDbContext> options)
            : base(options) { }

        // Models (MD)
        public DbSet<MDAccountList> MDAccountLists { get; set; }
        public DbSet<MDAccountInformation> MDAccountInformations { get; set; }
        public DbSet<MDUSD> MDUsds { get; set; }
        public DbSet<MDEpochs> MDEpochs { get; set; }
        public DbSet<MDCommitteeVotes> MDCommitteeVotes { get; set; }
        public DbSet<MDCommitteeInformation> MDCommitteeInformations { get; set; }
        public DbSet<MDQueryChainTip> MDQueryChainTips { get; set; }
        public DbSet<MDEpochProtocolParameters> MDEpochProtocolParameters { get; set; }
        public DbSet<MDTreasuryWithdrawals> MDTreasuryWithdrawals { get; set; }
        public DbSet<MDPoolInformation> MDPoolInformations { get; set; }
        public DbSet<MDVoteList> MDVoteLists { get; set; }
        public DbSet<MDProposalVotes> MDProposalVotes { get; set; }
        public DbSet<MDProposalVotingSummary> MDProposalVotingSummaries { get; set; }
        public DbSet<MDProposalsList> MDProposalsLists { get; set; }
        public DbSet<MDUtxoInfo> MDUtxoInfos { get; set; }
        public DbSet<MDPoolUpdates> MDPoolUpdates { get; set; }
        public DbSet<MDPoolDelegators> MDPoolDelegators { get; set; }
        public DbSet<MDVotersProposalList> MDVotersProposalLists { get; set; }
        public DbSet<MDPoolsVotingPowerHistory> MDPoolsVotingPowerHistories { get; set; }
        public DbSet<MDDreps> MDDreps { get; set; }
        public DbSet<MDDrepsVotes> MDDrepsVotes { get; set; }
        public DbSet<MDDrepsUpdates> MDDrepsUpdates { get; set; }
        public DbSet<MDDrepsDelegators> MDDrepsDelegators { get; set; }
        public DbSet<MDDrepsMetadata> MDDrepsMetadata { get; set; }
        public DbSet<MDDrepsInfo> MDDrepsInfos { get; set; }
        public DbSet<MDDrepsVotingPowerHistory> MDDrepsVotingPowerHistories { get; set; }
        public DbSet<MDDrepsList> MDDrepsLists { get; set; }
        public DbSet<MDPoolMetadata> MDPoolMetadata { get; set; }
        public DbSet<MDPoolStakeSnapshot> MDPoolStakeSnapshots { get; set; }
        public DbSet<MDPoolList> MDPoolLists { get; set; }
        public DbSet<MDTotals> MDTotals { get; set; }
        public DbSet<MDDrepsEpochSummary> MDDrepsEpochSummaries { get; set; }
        public DbSet<MDEpoch> MDEpoch { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<MDAccountUpdates> MDAccountUpdates { get; set; }
        public DbSet<GeneratedImage> GeneratedImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Lấy danh sách entities trước để tránh lỗi collection modification
            var entityTypes = modelBuilder.Model.GetEntityTypes().ToList();

            // Cấu hình tất cả entity là keyless (HasNoKey)
            foreach (var entity in entityTypes)
            {
                modelBuilder.Entity(entity.Name).HasNoKey();

                // Đặt tên table với tiền tố md_ hoặc ag_
                var tableName = GetTableNameWithPrefix(entity.ClrType.Name);
                modelBuilder.Entity(entity.Name).ToTable(tableName);

                // Ignore all navigation properties vì chúng ta dùng keyless entities
                var navigations = entity.GetNavigations().ToList();
                foreach (var navigation in navigations)
                {
                    modelBuilder.Entity(entity.Name).Ignore(navigation.Name);
                }
            }

            // Configure strategic indexes for optimal query performance
            ConfigurePerformanceIndexes(modelBuilder);
        }

        private static string GetTableNameWithPrefix(string className)
        {
            string tableName;

            if (className.StartsWith("MD"))
            {
                // Models: MD{ClassName} -> md_{class_name}
                tableName = "md_" + ToSnakeCase(className.Substring(2));
            }
            else if (className.StartsWith("AG"))
            {
                // AggregateModels: AG{ClassName} -> ag_{class_name}
                tableName = "ag_" + ToSnakeCase(className.Substring(2));
            }
            else
            {
                // Fallback: {ClassName} -> {class_name}
                tableName = ToSnakeCase(className);
            }

            return tableName;
        }

        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var startUnderscores = System.Text.RegularExpressions.Regex.Match(input, @"^_+");
            return startUnderscores
                + System
                    .Text.RegularExpressions.Regex.Replace(
                        input.Substring(startUnderscores.Length),
                        @"([a-z0-9])([A-Z])",
                        "$1_$2"
                    )
                    .ToLower();
        }

        /// <summary>
        /// Configure strategic database indexes for optimal query performance
        /// Based on analysis of MainAPI query patterns and sync job requirements
        /// </summary>
        private void ConfigurePerformanceIndexes(ModelBuilder modelBuilder)
        {
            // Note: Since we use keyless entities, we cannot use EF Core's HasIndex()
            // These indexes should be created via database migrations or SQL scripts
            // This method documents the required indexes for reference

            /*
            CRITICAL INDEXES FOR MAINAPI QUERY PERFORMANCE:

            -- DRep related indexes (high frequency queries)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_info_drep_id ON md_dreps_info(drep_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_info_active ON md_dreps_info(active);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_metadata_drep_id ON md_dreps_metadata(drep_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_voting_power_history_drep_id_epoch ON md_dreps_voting_power_history(drep_id, epoch_no DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_voting_power_history_epoch_desc ON md_dreps_voting_power_history(epoch_no DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_delegators_drep_id ON md_dreps_delegators(drep_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_delegators_stake_address ON md_dreps_delegators(stake_address);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_updates_drep_id ON md_dreps_updates(drep_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_updates_action_block_time ON md_dreps_updates(action, block_time DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_epoch_summary_epoch_desc ON md_dreps_epoch_summary(epoch_no DESC);

            -- Pool related indexes (high frequency queries)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pool_list_pool_id_bech32 ON md_pool_list(pool_id_bech32);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pool_list_active_epoch ON md_pool_list(active_epoch_no DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pool_list_pool_status ON md_pool_list(pool_status);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pool_information_pool_id_bech32 ON md_pool_information(pool_id_bech32);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pool_delegators_pool_id ON md_pool_delegators(pool_id_bech32);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pool_delegators_stake_address ON md_pool_delegators(stake_address);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pool_voting_power_history_epoch_desc ON md_pools_voting_power_history(epoch_no DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pool_updates_pool_id ON md_pool_updates(pool_id_bech32);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pool_stake_snapshot_pool_id ON md_pool_stake_snapshot(pool_id_bech32);

            -- Proposal related indexes (governance queries)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposals_list_proposal_id ON md_proposals_list(proposal_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposals_list_proposed_epoch ON md_proposals_list(proposed_epoch DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposals_list_expired_enacted ON md_proposals_list(expired_epoch, enacted_epoch);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposals_list_block_time ON md_proposals_list(block_time DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposal_votes_proposal_id ON md_proposal_votes(proposal_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposal_votes_voter_id ON md_proposal_votes(voter_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposal_votes_voter_role ON md_proposal_votes(voter_role);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposal_voting_summary_proposal_id ON md_proposal_voting_summary(proposal_id);

            -- Vote related indexes (voting history queries)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_vote_list_voter_id ON md_vote_list(voter_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_vote_list_voter_role ON md_vote_list(voter_role);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_vote_list_epoch_no ON md_vote_list(epoch_no DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_vote_list_block_time ON md_vote_list(block_time DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_vote_list_proposal_id ON md_vote_list(proposal_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_voters_proposal_list_proposal_id ON md_voters_proposal_list(proposal_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_voters_proposal_list_proposed_epoch ON md_voters_proposal_list(proposed_epoch DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_voters_proposal_list_block_time ON md_voters_proposal_list(block_time DESC);

            -- Epoch related indexes (time-based queries)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_epochs_no_desc ON md_epochs(no DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_epochs_start_time ON md_epochs(start_time DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_epochs_end_time ON md_epochs(end_time DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_epoch_no_desc ON md_epoch(epoch_no DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_epoch_protocol_parameters_epoch ON md_epoch_protocol_parameters(epoch_no DESC);

            -- Committee related indexes
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_committee_information_committee_id ON md_committee_information(committee_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_committee_votes_committee_id ON md_committee_votes(committee_id);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_committee_votes_proposal_id ON md_committee_votes(proposal_id);

            -- Account related indexes (delegation tracking)
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_account_updates_stake_address ON md_account_updates(stake_address);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_account_updates_action_type ON md_account_updates(action_type);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_account_updates_epoch_no ON md_account_updates(epoch_no DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_account_updates_block_time ON md_account_updates(block_time DESC);

            -- UTXO related indexes
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_utxo_info_stake_address ON md_utxo_info(stake_address);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_utxo_info_tx_hash ON md_utxo_info(tx_hash);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_utxo_info_epoch_no ON md_utxo_info(epoch_no DESC);

            -- Composite indexes for complex queries
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_voting_power_epoch_drep_composite ON md_dreps_voting_power_history(epoch_no DESC, drep_id, amount DESC);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_vote_list_epoch_voter_composite ON md_vote_list(epoch_no DESC, voter_id, voter_role);
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_proposals_governance_era ON md_proposals_list(proposed_epoch DESC, expired_epoch, enacted_epoch) WHERE proposed_epoch >= 507;
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_active_voting_power ON md_dreps_voting_power_history(epoch_no DESC, amount DESC) WHERE drep_id LIKE 'drep1%';

            -- Text search indexes for search functionality
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dreps_metadata_gin_search ON md_dreps_metadata USING gin(to_tsvector('english', meta_json));
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_pool_metadata_gin_search ON md_pool_metadata USING gin(to_tsvector('english', meta_json));
            */
        }
    }
}
