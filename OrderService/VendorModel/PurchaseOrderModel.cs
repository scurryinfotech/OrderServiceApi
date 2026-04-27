namespace OrderService.VendorModel
{
    public class PurchaseOrderModel
    {
        public int PurchaseOrderId { get; set; }
        public int VendorId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AdvancePaid { get; set; }
        public decimal RemainingAmount { get; set; }  // computed column
        public string Notes { get; set; }
        public string Status { get; set; }  // Pending | PartiallyPaid | Paid
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByStaffId { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        // From JOINs
        public string VendorName { get; set; }
        public string CreatedByName { get; set; }
    }
}
