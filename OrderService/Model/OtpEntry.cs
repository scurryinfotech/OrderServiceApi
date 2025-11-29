namespace OrderService.Model
{
    public class OtpEntry
    {
        public string PhoneNumber { get; set; }
        public string Otp { get; set; }
        public DateTime Expiry { get; set; }
    }
}
