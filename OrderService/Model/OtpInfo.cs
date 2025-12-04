namespace OrderService.Model
{
    public class OtpInfo
    {
        public string Otp { get; set; }
        public DateTime ExpiryTime { get; set; }
        public DateTime LastSentTime { get; set; }
        public int FailedAttempts { get; set; }
    }
}
