namespace BillMate.DTOs.PubSub
{
    public class MessageData
    {
        public string Data { get; set; }
        public string MessageId { get; set; }
        public DateTime PublishTime { get; set; }
    }
}
