namespace MainAPI.Application.Common.Constants
{
    public static class CacheKeys
    {
        // Membership data cache keys
        public const string TOTAL_MEMBERSHIP_DATA = "total_membership_data";
        public const string PARTICIPATE_IN_VOTING = "participate_in_voting";
        public const string GOVERNANCE_PARAMETERS = "governance_parameters";
        public const string ALLOCATION_DATA = "allocation_data";

        // Account cache keys
        public const string TOTAL_STAKE_ADDRESSES = "total_stake_addresses";

        // Committee cache keys
        public const string TOTAL_COMMITTEE = "total_committee";

        // Drep cache keys
        public const string TOTAL_DREP = "total_drep";
        public const string TOTAL_STAKE_NUMBERS = "total_stake_numbers";
        public const string DREP_INFO = "drep_info";
        public const string DREP_METADATA = "drep_metadata";
        public const string DREP_LIST = "drep_list";
        public const string DREP_VOTING_POWER_HISTORY = "drep_voting_power_history";
        public const string TOP_10_DREP_VOTING_POWER = "top_10_drep_voting_power";
        public const string DREP_CARD_DATA = "drep_card_data";
        public const string DREP_CARD_DATA_BY_ID = "drep_card_data_by_id";
        public const string DREP_VOTE_INFO = "drep_vote_info";
        public const string DREP_DELEGATION = "drep_delegation";
        public const string DREP_REGISTRATION = "drep_registration";
        public const string DREP_DETAILS_VOTING_POWER = "drep_details_voting_power";
        public const string DREPS_VOTING_POWER = "dreps_voting_power";
        public const string DREP_NEW_REGISTER = "drep_new_register";
        public const string DREP_AND_POOL_VOTING_THRESHOLD = "drep_and_pool_voting_threshold";
        public const string DREP_TOTAL_STAKE_APPROVAL_THRESHOLD = "drep_total_stake_approval_threshold";
        public const string TOTAL_WALLET_STATISTICS = "total_wallet_statistics";
        public const string DREP_HISTORY = "drep_history";
        public const string DREP_UPDATES = "drep_updates";
        public const string DREP_VOTES = "drep_votes";
        public const string DREP_DELEGATORS = "drep_delegators";
        public const string DREP_EPOCH_SUMMARY = "drep_epoch_summary";

        // Proposal cache keys
        public const string PROPOSAL_STATS = "proposal_stats";
        public const string PROPOSAL_VOTES = "proposal_votes";
        public const string PROPOSAL_ACTION_TYPE = "proposal_action_type";
        public const string GOVERNANCE_ACTIONS_STATISTICS = "governance_actions_statistics";
        public const string PROPOSAL_LIVE = "proposal_live";
        public const string PROPOSAL_EXPIRED = "proposal_expired";
        public const string GOVERNANCE_ACTIONS_STATISTICS_BY_EPOCH = "governance_actions_statistics_by_epoch";
        public const string PROPOSAL_VOTING_SUMMARY = "proposal_voting_summary";
        public const string PROPOSAL_LIVE_DETAIL = "proposal_live_detail";
        public const string PROPOSAL_EXPIRED_DETAIL = "proposal_expired_detail";

        // New consolidated proposal cache keys
        public const string PROPOSAL_COMBINED = "proposal_combined";
        public const string PROPOSAL_DETAIL_COMBINED = "proposal_detail_combined";

        // Epoch cache keys
        public const string CURRENT_EPOCH_INFO = "current_epoch_info";
        public const string TOTAL_STAKE = "total_stake";
        public const string EPOCH_INFO = "epoch_info";
        public const string EPOCH_INFO_SPO = "epoch_info_spo";
        public const string CURRENT_EPOCH = "current_epoch";

        // Pool cache keys
        public const string POOL_INFO = "pool_info";
        public const string POOL_LIST = "pool_list";
        public const string TOTAL_POOL = "total_pool";
        public const string ADA_STATISTICS = "ada_statistics";
        public const string ADA_STATISTICS_PERCENTAGE = "ada_statistics_percentage";
        public const string SPO_VOTING_POWER_HISTORY = "spo_voting_power_history";
        public const string TOTALS = "totals";
        public const string POOL_STAKE_SNAPSHOT = "pool_stake_snapshot";

        // Treasury cache keys
        public const string TOTAL_TREASURY = "total_treasury";
        public const string TREASURY_VOLATILITY = "treasury_volatility";
        public const string TREASURY_WITHDRAWALS = "treasury_withdrawals";

        // Voting cache keys
        public const string VOTING_CARDS_DATA = "voting_cards_data";
        public const string VOTING_HISTORY = "voting_history";
        public const string VOTE_LIST = "vote_list";
        public const string VOTE_STATISTIC_DREP_SPO = "vote_statistic_drep_spo";
        public const string VOTE_PARTICIPATION_INDEX = "vote_participation_index";

        // Image cache keys
        public const string IMAGE_GENERATION = "image_generation";
    }
}