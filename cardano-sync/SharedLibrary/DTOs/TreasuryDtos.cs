namespace SharedLibrary.DTOs
{
    public class TreasuryDataResponseDto
    {
        public double? treasury { get; set; }
        public double? total_withdrawals { get; set; }
        public List<int>? chart_stats { get; set; }
    }

    public class TreasuryResponseDto
    {
        public List<TreasuryVolatilityResponseDto> volatilities { get; set; } = new();
        public List<TreasuryWithdrawalsResponseDto> withdrawals { get; set; } = new();
    }

    public class TreasuryVolatilityResponseDto
    {
        public int? epoch_no { get; set; }
        public double? treasury { get; set; }
        public double? treasury_usd { get; set; }
    }

    public class TreasuryWithdrawalsResponseDto
    {
        public int? epoch_no { get; set; }
        public double? amount { get; set; }
    }
}