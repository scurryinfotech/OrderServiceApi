namespace OrderService.Model
{
    public class RazorpayOrderApiResponse
    {
        public string Id { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; }
        public string Receipt { get; set; }
        public string Status { get; set; }
    }
}
