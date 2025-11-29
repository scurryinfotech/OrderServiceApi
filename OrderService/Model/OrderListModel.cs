namespace OrderService.Model
{
    public class OrderListModel
    {
        public int TableNo { get; set; }
        public int Id { get; set; }
        public string ItemName { get; set; }
        public int HalfPortion { get; set; }
        public int FullPortion { get; set; }
        public decimal Price { get; set; }
        public int OrderStatusId { get; set; }
        public string OrderStatus { get; set; }
        public DateTime Date { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string OrderId { get; set; }
        public int StatusId { get; set; } // <-- Add this property
    }
}
