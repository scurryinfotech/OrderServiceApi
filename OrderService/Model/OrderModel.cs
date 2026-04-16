namespace OrderService.Model
{
    public class OrderModel
    {
        public int? selectedTable { get; set; }
        public int? userName { get; set; }
        public string customerName { get; set; }
        public string userPhone { get; set; }
        public string? OrderType { get; set; }
        public string? Address { get; set; }
        public string? specialInstruction { get; set; }
        public string? deliveryType { get; set; }
        public int userId { get; set; }

        // ↓ New payment fields
        public string? PaymentMode { get; set; }  
        public string? PaymentStatus { get; set; }  
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public string? RazorpaySignature { get; set; }

        public List<OrderItem> orderItems { get; set; }

        public class OrderItem
        {
            public int full { get; set; }
            public int half { get; set; }
            public int item_id { get; set; }
            public decimal Price { get; set; }
        }
    }
}