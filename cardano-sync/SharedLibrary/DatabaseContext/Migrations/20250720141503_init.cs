using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SharedLibrary.DatabaseContext.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_key",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp", nullable: true),
                    TotalRequests = table.Column<int>(type: "integer", nullable: false),
                    DailyRequests = table.Column<int>(type: "integer", nullable: false),
                    LastDailyReset = table.Column<DateTime>(type: "timestamp", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AllowedOrigins = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AllowedEndpoints = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_account_information",
                columns: table => new
                {
                    stake_address = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    delegated_drep = table.Column<string>(type: "jsonb", nullable: true),
                    delegated_pool = table.Column<string>(type: "jsonb", nullable: true),
                    total_balance = table.Column<string>(type: "text", nullable: true),
                    utxo = table.Column<string>(type: "text", nullable: true),
                    rewards = table.Column<string>(type: "text", nullable: true),
                    withdrawals = table.Column<string>(type: "text", nullable: true),
                    rewards_available = table.Column<string>(type: "text", nullable: true),
                    deposit = table.Column<string>(type: "text", nullable: true),
                    reserves = table.Column<string>(type: "text", nullable: true),
                    treasury = table.Column<string>(type: "text", nullable: true),
                    proposal_refund = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_account_list",
                columns: table => new
                {
                    stake_address = table.Column<string>(type: "text", nullable: true),
                    stake_address_hex = table.Column<string>(type: "text", nullable: true),
                    script_hash = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_account_updates",
                columns: table => new
                {
                    stake_address = table.Column<string>(type: "text", nullable: true),
                    drep_id = table.Column<string>(type: "text", nullable: true),
                    tx_hash = table.Column<string>(type: "text", nullable: true),
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    block_time = table.Column<long>(type: "bigint", nullable: true),
                    epoch_slot = table.Column<int>(type: "integer", nullable: true),
                    action_type = table.Column<string>(type: "text", nullable: true),
                    absolute_slot = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_committee_information",
                columns: table => new
                {
                    proposal_id = table.Column<string>(type: "text", nullable: true),
                    proposal_tx_hash = table.Column<string>(type: "text", nullable: true),
                    proposal_index = table.Column<int>(type: "integer", nullable: true),
                    quorum_numerator = table.Column<int>(type: "integer", nullable: true),
                    quorum_denominator = table.Column<int>(type: "integer", nullable: true),
                    members = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_committee_votes",
                columns: table => new
                {
                    proposal_id = table.Column<string>(type: "text", nullable: true),
                    proposal_tx_hash = table.Column<string>(type: "text", nullable: true),
                    proposal_index = table.Column<int>(type: "integer", nullable: true),
                    vote_tx_hash = table.Column<string>(type: "text", nullable: true),
                    block_time = table.Column<long>(type: "bigint", nullable: true),
                    vote = table.Column<string>(type: "text", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true),
                    cc_hot_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_dreps",
                columns: table => new
                {
                    hash = table.Column<string>(type: "text", nullable: true),
                    bech32_legacy = table.Column<string>(type: "text", nullable: true),
                    has_script = table.Column<bool>(type: "boolean", nullable: true),
                    tx_hash = table.Column<string>(type: "text", nullable: true),
                    url = table.Column<string>(type: "text", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    payment_address = table.Column<string>(type: "text", nullable: true),
                    given_name = table.Column<string>(type: "text", nullable: true),
                    objectives = table.Column<string>(type: "text", nullable: true),
                    motivations = table.Column<string>(type: "text", nullable: true),
                    qualifications = table.Column<string>(type: "text", nullable: true),
                    live_stake = table.Column<string>(type: "text", nullable: true),
                    delegator = table.Column<int>(type: "integer", nullable: true),
                    tx_time = table.Column<long>(type: "bigint", nullable: true),
                    last_active_epoch = table.Column<int>(type: "integer", nullable: true),
                    bech32 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_dreps_delegators",
                columns: table => new
                {
                    drep_id = table.Column<string>(type: "text", nullable: true),
                    stake_address = table.Column<string>(type: "text", nullable: true),
                    stake_address_hex = table.Column<string>(type: "text", nullable: true),
                    script_hash = table.Column<string>(type: "jsonb", nullable: true),
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_dreps_epoch_summary",
                columns: table => new
                {
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<string>(type: "text", nullable: true),
                    dreps = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_dreps_info",
                columns: table => new
                {
                    drep_id = table.Column<string>(type: "text", nullable: true),
                    hex = table.Column<string>(type: "text", nullable: true),
                    has_script = table.Column<bool>(type: "boolean", nullable: true),
                    registered = table.Column<bool>(type: "boolean", nullable: true),
                    deposit = table.Column<string>(type: "jsonb", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: true),
                    expires_epoch_no = table.Column<string>(type: "jsonb", nullable: true),
                    amount = table.Column<string>(type: "text", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_dreps_list",
                columns: table => new
                {
                    drep_id = table.Column<string>(type: "text", nullable: true),
                    hex = table.Column<string>(type: "text", nullable: true),
                    has_script = table.Column<bool>(type: "boolean", nullable: true),
                    registered = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_dreps_metadata",
                columns: table => new
                {
                    drep_id = table.Column<string>(type: "text", nullable: true),
                    hex = table.Column<string>(type: "text", nullable: true),
                    has_script = table.Column<bool>(type: "boolean", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true),
                    meta_json = table.Column<string>(type: "jsonb", nullable: true),
                    bytes = table.Column<string>(type: "jsonb", nullable: true),
                    warning = table.Column<string>(type: "jsonb", nullable: true),
                    language = table.Column<string>(type: "jsonb", nullable: true),
                    comment = table.Column<string>(type: "jsonb", nullable: true),
                    is_valid = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_dreps_updates",
                columns: table => new
                {
                    drep_id = table.Column<string>(type: "text", nullable: true),
                    hex = table.Column<string>(type: "text", nullable: true),
                    has_script = table.Column<bool>(type: "boolean", nullable: true),
                    update_tx_hash = table.Column<string>(type: "text", nullable: true),
                    cert_index = table.Column<int>(type: "integer", nullable: true),
                    block_time = table.Column<long>(type: "bigint", nullable: true),
                    action = table.Column<string>(type: "text", nullable: true),
                    deposit = table.Column<string>(type: "jsonb", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true),
                    meta_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_dreps_votes",
                columns: table => new
                {
                    proposal_id = table.Column<string>(type: "text", nullable: true),
                    proposal_tx_hash = table.Column<string>(type: "text", nullable: true),
                    proposal_index = table.Column<int>(type: "integer", nullable: true),
                    vote_tx_hash = table.Column<string>(type: "text", nullable: true),
                    block_time = table.Column<long>(type: "bigint", nullable: true),
                    vote = table.Column<string>(type: "text", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_dreps_voting_power_history",
                columns: table => new
                {
                    drep_id = table.Column<string>(type: "text", nullable: true),
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_epoch",
                columns: table => new
                {
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    out_sum = table.Column<string>(type: "text", nullable: true),
                    fees = table.Column<string>(type: "text", nullable: true),
                    tx_count = table.Column<int>(type: "integer", nullable: true),
                    blk_count = table.Column<int>(type: "integer", nullable: true),
                    start_time = table.Column<long>(type: "bigint", nullable: true),
                    end_time = table.Column<long>(type: "bigint", nullable: true),
                    first_block_time = table.Column<long>(type: "bigint", nullable: true),
                    last_block_time = table.Column<long>(type: "bigint", nullable: true),
                    active_stake = table.Column<string>(type: "jsonb", nullable: true),
                    total_rewards = table.Column<string>(type: "jsonb", nullable: true),
                    avg_blk_reward = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_epoch_protocol_parameters",
                columns: table => new
                {
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    min_fee_a = table.Column<string>(type: "jsonb", nullable: true),
                    min_fee_b = table.Column<string>(type: "jsonb", nullable: true),
                    max_block_size = table.Column<string>(type: "jsonb", nullable: true),
                    max_tx_size = table.Column<string>(type: "jsonb", nullable: true),
                    max_bh_size = table.Column<string>(type: "jsonb", nullable: true),
                    key_deposit = table.Column<string>(type: "jsonb", nullable: true),
                    pool_deposit = table.Column<string>(type: "jsonb", nullable: true),
                    max_epoch = table.Column<string>(type: "jsonb", nullable: true),
                    optimal_pool_count = table.Column<string>(type: "jsonb", nullable: true),
                    influence = table.Column<string>(type: "jsonb", nullable: true),
                    monetary_expand_rate = table.Column<string>(type: "jsonb", nullable: true),
                    treasury_growth_rate = table.Column<string>(type: "jsonb", nullable: true),
                    decentralisation = table.Column<string>(type: "jsonb", nullable: true),
                    extra_entropy = table.Column<string>(type: "jsonb", nullable: true),
                    protocol_major = table.Column<string>(type: "jsonb", nullable: true),
                    protocol_minor = table.Column<string>(type: "jsonb", nullable: true),
                    min_utxo_value = table.Column<string>(type: "jsonb", nullable: true),
                    min_pool_cost = table.Column<string>(type: "jsonb", nullable: true),
                    nonce = table.Column<string>(type: "jsonb", nullable: true),
                    block_hash = table.Column<string>(type: "text", nullable: true),
                    cost_models = table.Column<string>(type: "jsonb", nullable: true),
                    price_mem = table.Column<string>(type: "jsonb", nullable: true),
                    price_step = table.Column<string>(type: "jsonb", nullable: true),
                    max_tx_ex_mem = table.Column<string>(type: "jsonb", nullable: true),
                    max_tx_ex_steps = table.Column<string>(type: "jsonb", nullable: true),
                    max_block_ex_mem = table.Column<string>(type: "jsonb", nullable: true),
                    max_block_ex_steps = table.Column<string>(type: "jsonb", nullable: true),
                    max_val_size = table.Column<string>(type: "jsonb", nullable: true),
                    collateral_percent = table.Column<string>(type: "jsonb", nullable: true),
                    max_collateral_inputs = table.Column<string>(type: "jsonb", nullable: true),
                    coins_per_utxo_size = table.Column<string>(type: "jsonb", nullable: true),
                    pvt_motion_no_confidence = table.Column<string>(type: "jsonb", nullable: true),
                    pvt_committee_normal = table.Column<string>(type: "jsonb", nullable: true),
                    pvt_committee_no_confidence = table.Column<string>(type: "jsonb", nullable: true),
                    pvt_hard_fork_initiation = table.Column<string>(type: "jsonb", nullable: true),
                    dvt_motion_no_confidence = table.Column<string>(type: "jsonb", nullable: true),
                    dvt_committee_normal = table.Column<string>(type: "jsonb", nullable: true),
                    dvt_committee_no_confidence = table.Column<string>(type: "jsonb", nullable: true),
                    dvt_update_to_constitution = table.Column<string>(type: "jsonb", nullable: true),
                    dvt_hard_fork_initiation = table.Column<string>(type: "jsonb", nullable: true),
                    dvt_p_p_network_group = table.Column<string>(type: "jsonb", nullable: true),
                    dvt_p_p_economic_group = table.Column<string>(type: "jsonb", nullable: true),
                    dvt_p_p_technical_group = table.Column<string>(type: "jsonb", nullable: true),
                    dvt_p_p_gov_group = table.Column<string>(type: "jsonb", nullable: true),
                    dvt_treasury_withdrawal = table.Column<string>(type: "jsonb", nullable: true),
                    committee_min_size = table.Column<string>(type: "jsonb", nullable: true),
                    committee_max_term_length = table.Column<string>(type: "jsonb", nullable: true),
                    gov_action_lifetime = table.Column<string>(type: "jsonb", nullable: true),
                    gov_action_deposit = table.Column<string>(type: "jsonb", nullable: true),
                    drep_deposit = table.Column<string>(type: "jsonb", nullable: true),
                    drep_activity = table.Column<string>(type: "jsonb", nullable: true),
                    pvtpp_security_group = table.Column<string>(type: "jsonb", nullable: true),
                    min_fee_ref_script_cost_per_byte = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_epochs",
                columns: table => new
                {
                    no = table.Column<int>(type: "integer", nullable: true),
                    tx_amount = table.Column<string>(type: "text", nullable: true),
                    circulating_supply = table.Column<string>(type: "text", nullable: true),
                    pool = table.Column<int>(type: "integer", nullable: true),
                    pool_with_block = table.Column<int>(type: "integer", nullable: true),
                    pool_with_stake = table.Column<int>(type: "integer", nullable: true),
                    pool_fee = table.Column<string>(type: "text", nullable: true),
                    reward_amount = table.Column<string>(type: "text", nullable: true),
                    stake = table.Column<string>(type: "text", nullable: true),
                    delegator = table.Column<int>(type: "integer", nullable: true),
                    account = table.Column<int>(type: "integer", nullable: true),
                    account_with_reward = table.Column<int>(type: "integer", nullable: true),
                    pool_register = table.Column<int>(type: "integer", nullable: true),
                    pool_retire = table.Column<int>(type: "integer", nullable: true),
                    orphaned_reward_amount = table.Column<string>(type: "text", nullable: true),
                    block_with_tx = table.Column<int>(type: "integer", nullable: true),
                    byron = table.Column<int>(type: "integer", nullable: true),
                    byron_with_amount = table.Column<int>(type: "integer", nullable: true),
                    byron_amount = table.Column<string>(type: "text", nullable: true),
                    account_with_amount = table.Column<int>(type: "integer", nullable: true),
                    delegator_with_stake = table.Column<int>(type: "integer", nullable: true),
                    token = table.Column<int>(type: "integer", nullable: true),
                    token_policy = table.Column<int>(type: "integer", nullable: true),
                    token_holder = table.Column<int>(type: "integer", nullable: true),
                    token_tx = table.Column<int>(type: "integer", nullable: true),
                    out_sum = table.Column<string>(type: "text", nullable: true),
                    fees = table.Column<string>(type: "text", nullable: true),
                    tx = table.Column<int>(type: "integer", nullable: true),
                    block = table.Column<int>(type: "integer", nullable: true),
                    start_time = table.Column<long>(type: "bigint", nullable: true),
                    end_time = table.Column<long>(type: "bigint", nullable: true),
                    optimal_pool_count = table.Column<int>(type: "integer", nullable: true),
                    decentralisation = table.Column<double>(type: "double precision", nullable: true),
                    nonce = table.Column<string>(type: "text", nullable: true),
                    holder_range = table.Column<string>(type: "jsonb", nullable: true),
                    exchange_rate = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_pool_delegators",
                columns: table => new
                {
                    pool_id_bech32 = table.Column<string>(type: "text", nullable: false),
                    stake_address = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<string>(type: "text", nullable: true),
                    active_epoch_no = table.Column<int>(type: "integer", nullable: true),
                    latest_delegation_tx_hash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_pool_information",
                columns: table => new
                {
                    pool_id_bech32 = table.Column<string>(type: "text", nullable: true),
                    pool_id_hex = table.Column<string>(type: "text", nullable: true),
                    active_epoch_no = table.Column<int>(type: "integer", nullable: true),
                    vrf_key_hash = table.Column<string>(type: "text", nullable: true),
                    margin = table.Column<double>(type: "double precision", nullable: true),
                    fixed_cost = table.Column<string>(type: "text", nullable: true),
                    pledge = table.Column<string>(type: "text", nullable: true),
                    deposit = table.Column<string>(type: "text", nullable: true),
                    reward_addr = table.Column<string>(type: "text", nullable: true),
                    reward_addr_delegated_drep = table.Column<string>(type: "text", nullable: true),
                    owners = table.Column<string>(type: "jsonb", nullable: true),
                    relays = table.Column<string>(type: "jsonb", nullable: true),
                    meta_url = table.Column<string>(type: "text", nullable: true),
                    meta_hash = table.Column<string>(type: "text", nullable: true),
                    meta_json = table.Column<string>(type: "jsonb", nullable: true),
                    pool_status = table.Column<string>(type: "text", nullable: true),
                    retiring_epoch = table.Column<int>(type: "integer", nullable: true),
                    op_cert = table.Column<string>(type: "text", nullable: true),
                    op_cert_counter = table.Column<int>(type: "integer", nullable: true),
                    active_stake = table.Column<string>(type: "text", nullable: true),
                    sigma = table.Column<double>(type: "double precision", nullable: true),
                    block_count = table.Column<int>(type: "integer", nullable: true),
                    live_pledge = table.Column<string>(type: "text", nullable: true),
                    live_stake = table.Column<string>(type: "text", nullable: true),
                    live_delegators = table.Column<int>(type: "integer", nullable: true),
                    live_saturation = table.Column<double>(type: "double precision", nullable: true),
                    voting_power = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_pool_list",
                columns: table => new
                {
                    pool_id_bech32 = table.Column<string>(type: "text", nullable: true),
                    pool_id_hex = table.Column<string>(type: "text", nullable: true),
                    active_epoch_no = table.Column<string>(type: "text", nullable: true),
                    margin = table.Column<string>(type: "jsonb", nullable: true),
                    fixed_cost = table.Column<string>(type: "jsonb", nullable: true),
                    pledge = table.Column<string>(type: "jsonb", nullable: true),
                    deposit = table.Column<string>(type: "jsonb", nullable: true),
                    reward_addr = table.Column<string>(type: "jsonb", nullable: true),
                    owners = table.Column<string>(type: "jsonb", nullable: true),
                    relays = table.Column<string>(type: "jsonb", nullable: true),
                    ticker = table.Column<string>(type: "text", nullable: true),
                    pool_group = table.Column<string>(type: "text", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true),
                    pool_status = table.Column<string>(type: "text", nullable: true),
                    active_stake = table.Column<string>(type: "text", nullable: true),
                    retiring_epoch = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_pool_metadata",
                columns: table => new
                {
                    pool_id_bech32 = table.Column<string>(type: "text", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true),
                    meta_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_pool_stake_snapshot",
                columns: table => new
                {
                    pool_id_bech32 = table.Column<string>(type: "text", nullable: false),
                    snapshot = table.Column<string>(type: "text", nullable: true),
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    nonce = table.Column<string>(type: "jsonb", nullable: true),
                    pool_stake = table.Column<string>(type: "text", nullable: true),
                    active_stake = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_pool_updates",
                columns: table => new
                {
                    tx_hash = table.Column<string>(type: "text", nullable: true),
                    block_time = table.Column<long>(type: "bigint", nullable: true),
                    pool_id_bech32 = table.Column<string>(type: "text", nullable: true),
                    pool_id_hex = table.Column<string>(type: "text", nullable: true),
                    active_epoch_no = table.Column<string>(type: "text", nullable: true),
                    vrf_key_hash = table.Column<string>(type: "text", nullable: true),
                    margin = table.Column<string>(type: "text", nullable: true),
                    fixed_cost = table.Column<string>(type: "text", nullable: true),
                    pledge = table.Column<string>(type: "text", nullable: true),
                    reward_addr = table.Column<string>(type: "text", nullable: true),
                    owners = table.Column<string>(type: "jsonb", nullable: true),
                    relays = table.Column<string>(type: "jsonb", nullable: true),
                    meta_url = table.Column<string>(type: "text", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true),
                    meta_json = table.Column<string>(type: "jsonb", nullable: true),
                    update_type = table.Column<string>(type: "text", nullable: true),
                    retiring_epoch = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_pools_voting_power_history",
                columns: table => new
                {
                    pool_id_bech32 = table.Column<string>(type: "text", nullable: true),
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_proposal_votes",
                columns: table => new
                {
                    block_time = table.Column<long>(type: "bigint", nullable: true),
                    voter_role = table.Column<string>(type: "text", nullable: true),
                    voter_id = table.Column<string>(type: "text", nullable: true),
                    voter_hex = table.Column<string>(type: "text", nullable: true),
                    voter_has_script = table.Column<bool>(type: "boolean", nullable: true),
                    vote = table.Column<string>(type: "text", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true),
                    proposal_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_proposal_voting_summary",
                columns: table => new
                {
                    proposal_id = table.Column<string>(type: "text", nullable: true),
                    proposal_type = table.Column<string>(type: "text", nullable: true),
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    drep_yes_votes_cast = table.Column<int>(type: "integer", nullable: true),
                    drep_active_yes_vote_power = table.Column<string>(type: "text", nullable: true),
                    drep_yes_vote_power = table.Column<string>(type: "text", nullable: true),
                    drep_yes_pct = table.Column<double>(type: "double precision", nullable: true),
                    drep_no_votes_cast = table.Column<int>(type: "integer", nullable: true),
                    drep_active_no_vote_power = table.Column<string>(type: "text", nullable: true),
                    drep_no_vote_power = table.Column<string>(type: "text", nullable: true),
                    drep_no_pct = table.Column<double>(type: "double precision", nullable: true),
                    drep_abstain_votes_cast = table.Column<int>(type: "integer", nullable: true),
                    drep_active_abstain_vote_power = table.Column<string>(type: "text", nullable: true),
                    drep_always_no_confidence_vote_power = table.Column<string>(type: "text", nullable: true),
                    drep_always_abstain_vote_power = table.Column<string>(type: "text", nullable: true),
                    pool_yes_votes_cast = table.Column<int>(type: "integer", nullable: true),
                    pool_active_yes_vote_power = table.Column<string>(type: "text", nullable: true),
                    pool_yes_vote_power = table.Column<string>(type: "text", nullable: true),
                    pool_yes_pct = table.Column<double>(type: "double precision", nullable: true),
                    pool_no_votes_cast = table.Column<int>(type: "integer", nullable: true),
                    pool_active_no_vote_power = table.Column<string>(type: "text", nullable: true),
                    pool_no_vote_power = table.Column<string>(type: "text", nullable: true),
                    pool_no_pct = table.Column<double>(type: "double precision", nullable: true),
                    pool_abstain_votes_cast = table.Column<int>(type: "integer", nullable: true),
                    pool_active_abstain_vote_power = table.Column<string>(type: "text", nullable: true),
                    pool_passive_always_abstain_votes_assigned = table.Column<int>(type: "integer", nullable: true),
                    pool_passive_always_abstain_vote_power = table.Column<string>(type: "text", nullable: true),
                    pool_passive_always_no_confidence_votes_assigned = table.Column<int>(type: "integer", nullable: true),
                    pool_passive_always_no_confidence_vote_power = table.Column<string>(type: "text", nullable: true),
                    committee_yes_votes_cast = table.Column<int>(type: "integer", nullable: true),
                    committee_yes_pct = table.Column<double>(type: "double precision", nullable: true),
                    committee_no_votes_cast = table.Column<int>(type: "integer", nullable: true),
                    committee_no_pct = table.Column<double>(type: "double precision", nullable: true),
                    committee_abstain_votes_cast = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_proposals_list",
                columns: table => new
                {
                    block_time = table.Column<long>(type: "bigint", nullable: true),
                    proposal_id = table.Column<string>(type: "text", nullable: true),
                    proposal_tx_hash = table.Column<string>(type: "text", nullable: true),
                    proposal_index = table.Column<int>(type: "integer", nullable: true),
                    proposal_type = table.Column<string>(type: "text", nullable: true),
                    proposal_description = table.Column<string>(type: "jsonb", nullable: true),
                    deposit = table.Column<string>(type: "jsonb", nullable: true),
                    return_address = table.Column<string>(type: "text", nullable: true),
                    proposed_epoch = table.Column<int>(type: "integer", nullable: true),
                    ratified_epoch = table.Column<string>(type: "jsonb", nullable: true),
                    enacted_epoch = table.Column<string>(type: "jsonb", nullable: true),
                    dropped_epoch = table.Column<string>(type: "jsonb", nullable: true),
                    expired_epoch = table.Column<string>(type: "jsonb", nullable: true),
                    expiration = table.Column<string>(type: "jsonb", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true),
                    meta_json = table.Column<string>(type: "jsonb", nullable: true),
                    meta_comment = table.Column<string>(type: "jsonb", nullable: true),
                    meta_language = table.Column<string>(type: "jsonb", nullable: true),
                    meta_is_valid = table.Column<string>(type: "jsonb", nullable: true),
                    withdrawal = table.Column<string>(type: "jsonb", nullable: true),
                    param_proposal = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_query_chain_tip",
                columns: table => new
                {
                    hash = table.Column<string>(type: "text", nullable: true),
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    abs_slot = table.Column<int>(type: "integer", nullable: true),
                    epoch_slot = table.Column<int>(type: "integer", nullable: true),
                    block_time = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_totals",
                columns: table => new
                {
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    circulation = table.Column<string>(type: "text", nullable: true),
                    treasury = table.Column<string>(type: "text", nullable: true),
                    reward = table.Column<string>(type: "text", nullable: true),
                    supply = table.Column<string>(type: "text", nullable: true),
                    reserves = table.Column<string>(type: "text", nullable: true),
                    fees = table.Column<string>(type: "text", nullable: true),
                    deposits_stake = table.Column<string>(type: "text", nullable: true),
                    deposits_drep = table.Column<string>(type: "text", nullable: true),
                    deposits_proposal = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_treasury_withdrawals",
                columns: table => new
                {
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    sum = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_usd",
                columns: table => new
                {
                    cardano = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_utxo_info",
                columns: table => new
                {
                    tx_hash = table.Column<string>(type: "text", nullable: true),
                    tx_index = table.Column<int>(type: "integer", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    value = table.Column<string>(type: "text", nullable: true),
                    stake_address = table.Column<string>(type: "jsonb", nullable: true),
                    payment_cred = table.Column<string>(type: "jsonb", nullable: true),
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    block_height = table.Column<int>(type: "integer", nullable: true),
                    block_time = table.Column<long>(type: "bigint", nullable: true),
                    datum_hash = table.Column<string>(type: "jsonb", nullable: true),
                    inline_datum = table.Column<string>(type: "jsonb", nullable: true),
                    reference_script = table.Column<string>(type: "jsonb", nullable: true),
                    asset_list = table.Column<string>(type: "jsonb", nullable: true),
                    is_spent = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_vote_list",
                columns: table => new
                {
                    vote_tx_hash = table.Column<string>(type: "text", nullable: true),
                    voter_role = table.Column<string>(type: "text", nullable: true),
                    voter_id = table.Column<string>(type: "text", nullable: true),
                    proposal_id = table.Column<string>(type: "text", nullable: true),
                    proposal_tx_hash = table.Column<string>(type: "text", nullable: true),
                    proposal_index = table.Column<int>(type: "integer", nullable: true),
                    proposal_type = table.Column<string>(type: "text", nullable: true),
                    epoch_no = table.Column<int>(type: "integer", nullable: true),
                    block_height = table.Column<int>(type: "integer", nullable: true),
                    block_time = table.Column<long>(type: "bigint", nullable: true),
                    vote = table.Column<string>(type: "text", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true),
                    meta_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "md_voters_proposal_list",
                columns: table => new
                {
                    block_time = table.Column<long>(type: "bigint", nullable: true),
                    proposal_id = table.Column<string>(type: "text", nullable: true),
                    proposal_tx_hash = table.Column<string>(type: "text", nullable: true),
                    proposal_index = table.Column<int>(type: "integer", nullable: true),
                    proposal_type = table.Column<string>(type: "text", nullable: true),
                    proposal_description = table.Column<string>(type: "jsonb", nullable: true),
                    deposit = table.Column<string>(type: "jsonb", nullable: true),
                    return_address = table.Column<string>(type: "text", nullable: true),
                    proposed_epoch = table.Column<int>(type: "integer", nullable: true),
                    ratified_epoch = table.Column<string>(type: "jsonb", nullable: true),
                    enacted_epoch = table.Column<string>(type: "jsonb", nullable: true),
                    dropped_epoch = table.Column<string>(type: "jsonb", nullable: true),
                    expired_epoch = table.Column<string>(type: "jsonb", nullable: true),
                    expiration = table.Column<string>(type: "jsonb", nullable: true),
                    meta_url = table.Column<string>(type: "jsonb", nullable: true),
                    meta_hash = table.Column<string>(type: "jsonb", nullable: true),
                    meta_json = table.Column<string>(type: "jsonb", nullable: true),
                    meta_comment = table.Column<string>(type: "jsonb", nullable: true),
                    meta_language = table.Column<string>(type: "jsonb", nullable: true),
                    meta_is_valid = table.Column<string>(type: "jsonb", nullable: true),
                    withdrawal = table.Column<string>(type: "jsonb", nullable: true),
                    param_proposal = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_key");

            migrationBuilder.DropTable(
                name: "md_account_information");

            migrationBuilder.DropTable(
                name: "md_account_list");

            migrationBuilder.DropTable(
                name: "md_account_updates");

            migrationBuilder.DropTable(
                name: "md_committee_information");

            migrationBuilder.DropTable(
                name: "md_committee_votes");

            migrationBuilder.DropTable(
                name: "md_dreps");

            migrationBuilder.DropTable(
                name: "md_dreps_delegators");

            migrationBuilder.DropTable(
                name: "md_dreps_epoch_summary");

            migrationBuilder.DropTable(
                name: "md_dreps_info");

            migrationBuilder.DropTable(
                name: "md_dreps_list");

            migrationBuilder.DropTable(
                name: "md_dreps_metadata");

            migrationBuilder.DropTable(
                name: "md_dreps_updates");

            migrationBuilder.DropTable(
                name: "md_dreps_votes");

            migrationBuilder.DropTable(
                name: "md_dreps_voting_power_history");

            migrationBuilder.DropTable(
                name: "md_epoch");

            migrationBuilder.DropTable(
                name: "md_epoch_protocol_parameters");

            migrationBuilder.DropTable(
                name: "md_epochs");

            migrationBuilder.DropTable(
                name: "md_pool_delegators");

            migrationBuilder.DropTable(
                name: "md_pool_information");

            migrationBuilder.DropTable(
                name: "md_pool_list");

            migrationBuilder.DropTable(
                name: "md_pool_metadata");

            migrationBuilder.DropTable(
                name: "md_pool_stake_snapshot");

            migrationBuilder.DropTable(
                name: "md_pool_updates");

            migrationBuilder.DropTable(
                name: "md_pools_voting_power_history");

            migrationBuilder.DropTable(
                name: "md_proposal_votes");

            migrationBuilder.DropTable(
                name: "md_proposal_voting_summary");

            migrationBuilder.DropTable(
                name: "md_proposals_list");

            migrationBuilder.DropTable(
                name: "md_query_chain_tip");

            migrationBuilder.DropTable(
                name: "md_totals");

            migrationBuilder.DropTable(
                name: "md_treasury_withdrawals");

            migrationBuilder.DropTable(
                name: "md_usd");

            migrationBuilder.DropTable(
                name: "md_utxo_info");

            migrationBuilder.DropTable(
                name: "md_vote_list");

            migrationBuilder.DropTable(
                name: "md_voters_proposal_list");
        }
    }
}
