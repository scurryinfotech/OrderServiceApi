namespace OrderService.VendorModel
{
    public class VendorPaymentModel
    {
        public int PaymentId { get; set; }
        public int PurchaseOrderId { get; set; }
        public int VendorId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; }    
        public string PaymentMethod { get; set; }  
        public string InvoiceNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByStaffId { get; set; }
        public string ModifiedBy { get; set; }
        // From JOINs
        public string VendorName { get; set; }
        public string CreatedByName { get; set; }
    }
}
