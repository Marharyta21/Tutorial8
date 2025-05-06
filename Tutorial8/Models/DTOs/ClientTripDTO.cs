namespace Tutorial8.Models.DTOs
{
    public class ClientTripDTO
    {
        public int IdClient { get; set; }
        public int IdTrip { get; set; }
        public int RegisteredAt { get; set; }
        public int? PaymentDate { get; set; }
        public TripDTO Trip { get; set; }
    }
}