namespace Web.Api.Model
{
     public class CallEvent
    {
        public string serverCallId { get; set; }
        public string correlationId { get; set; }
        public bool isRoomsCall { get; set; }
    }
}
