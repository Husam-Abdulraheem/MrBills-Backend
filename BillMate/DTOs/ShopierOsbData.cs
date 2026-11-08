namespace BillMate.DTOs
{
    public class ShopierOsbData
    {
        public string email { get; set; }
        public string orderid { get; set; }
        public int currency { get; set; }
        public string price { get; set; }
        public string buyername { get; set; }
        public string buyersurname { get; set; }
        public int productcount { get; set; }
        public int productid { get; set; }
        public string productlist { get; set; }
        public List<ChartDetail> chartdetails { get; set; }
        public string customernote { get; set; }
        public int istest { get; set; }
    }
    public class ChartDetail
    {
        public int id { get; set; }
        public int quantity { get; set; }
    }
}
