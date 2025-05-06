using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services
{
    public class TripsService : ITripsService
    {
        private readonly string _connectionString;

        public TripsService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("TravelAgencyDb") ??
                               "Server=localhost,1433;Database=master;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";
        }
        
        public async Task<List<TripDTO>> GetTrips()
        {
            var trips = new List<TripDTO>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string tripSql = @"
                    SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople
                    FROM Trip t
                    ORDER BY t.DateFrom";

                using (SqlCommand command = new SqlCommand(tripSql, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var trip = new TripDTO
                            {
                                IdTrip = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                                DateFrom = reader.GetDateTime(3),
                                DateTo = reader.GetDateTime(4),
                                MaxPeople = reader.GetInt32(5),
                                Countries = new List<CountryDTO>()
                            };

                            trips.Add(trip);
                        }
                    }
                }
                
                foreach (var trip in trips)
                {
                    string countrySql = @"
                        SELECT c.IdCountry, c.Name
                        FROM Country c
                        JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry
                        WHERE ct.IdTrip = @IdTrip";

                    using (SqlCommand command = new SqlCommand(countrySql, connection))
                    {
                        command.Parameters.AddWithValue("@IdTrip", trip.IdTrip);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var country = new CountryDTO
                                {
                                    IdCountry = reader.GetInt32(0),
                                    Name = reader.GetString(1)
                                };

                                trip.Countries.Add(country);
                            }
                        }
                    }
                }
            }

            return trips;
        }

        public async Task<bool> DoesTripExist(int tripId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT COUNT(1) FROM Trip WHERE IdTrip = @IdTrip";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdTrip", tripId);
                    int count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        public async Task<bool> IsTripFull(int tripId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT t.MaxPeople, COUNT(ct.IdClient) as CurrentParticipants
                    FROM Trip t
                    LEFT JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
                    WHERE t.IdTrip = @IdTrip
                    GROUP BY t.MaxPeople";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdTrip", tripId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int maxPeople = reader.GetInt32(0);
                            int currentParticipants = reader.GetInt32(1);
                            return currentParticipants >= maxPeople;
                        }
                        return false;
                    }
                }
            }
        }
    }
}