namespace IMS.Models.DTO
{
    public class DeliveryChallanDto
    {
        public int Id { get; set; }
        public string ChallanNo { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverMobile { get; set; }
        public DateTime Date { get; set; }
        public string createdByName { get; set; }
        public List<DeliveryChallanItemDto> Items { get; set; }
    }

    public class DeliveryChallanItemDto
    {
        public string Particular { get; set; }
        public string ModelNo { get; set; }
        public int? Quantity { get; set; }
        public string Remarks { get; set; }
    }

}
