namespace OrderService.VendorModel
{
    public class VendorLedgerModel
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string CreatedByName { get; set; }
        public decimal TotalOrdered { get; set; }
        public decimal TotalAdvance { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
    }
}
