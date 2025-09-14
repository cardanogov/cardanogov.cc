using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDAccountInformation
    {
        public string? stake_address { get; set; } // Cardano staking address (bech32)
        public string? status { get; set; } // Stake address status (enum)
        [Column(TypeName = "jsonb")]
        public string? delegated_drep { get; set; } // JSONB: Delegation status to DRep (object)
        [Column(TypeName = "jsonb")]
        public string? delegated_pool { get; set; } // JSONB: Delegation status to Pool (object)
        public string? total_balance { get; set; } // Total balance (string)
        public string? utxo { get; set; } // UTxO balance (string)
        public string? rewards { get; set; } // Total rewards earned (string)
        public string? withdrawals { get; set; } // Total rewards withdrawn (string)
        public string? rewards_available { get; set; } // Rewards available for withdrawal (string)
        public string? deposit { get; set; } // Deposit available for withdrawal (string)
        public string? reserves { get; set; } // Reserves MIR value (string)
        public string? treasury { get; set; } // Treasury MIR value (string)
        public string? proposal_refund { get; set; } // Proposal refund (string)
    }
}