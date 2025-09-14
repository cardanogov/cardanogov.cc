namespace SharedLibrary.DTOs
{
    public class TotalStakeResponseDto
    {
        public double? totalADA { get; set; }
        public double? totalSupply { get; set; }
        public List<double>? chartStats { get; set; }
    }
}