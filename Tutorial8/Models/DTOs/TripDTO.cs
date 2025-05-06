namespace Tutorial8.Models.DTOs
{
    public class TripDTO
    {
        public int IdTrip { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int MaxPeople { get; set; }
        public List<CountryDTO> Countries { get; set; } = new List<CountryDTO>();
    }

    public class CountryDTO
    {
        public int IdCountry { get; set; }
        public string Name { get; set; }
    }
}