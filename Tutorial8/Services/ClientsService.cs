using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services
{
    public class ClientsService : IClientsService
    {
        private readonly string _connectionString;
        private readonly ITripsService _tripsService;

        public ClientsService(IConfiguration configuration, ITripsService tripsService)
        {
            _connectionString = configuration.GetConnectionString("TravelAgencyDb") ??
                               "Server=localhost,1433;Database=master;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";
            _tripsService = tripsService;
        }

        public async Task<List<ClientTripDTO>> GetClientTrips(int clientId)
        {
            var clientTrips = new List<ClientTripDTO>();

            if (!await DoesClientExist(clientId))
            {
                return clientTrips;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string tripsSql = @"
                    SELECT ct.IdTrip, ct.RegisteredAt, ct.PaymentDate,
                           t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople
                    FROM Client_Trip ct
                    JOIN Trip t ON ct.IdTrip = t.IdTrip
                    WHERE ct.IdClient = @IdClient";

                using (SqlCommand command = new SqlCommand(tripsSql, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var clientTrip = new ClientTripDTO
                            {
                                IdClient = clientId,
                                IdTrip = reader.GetInt32(0),
                                RegisteredAt = reader.GetInt32(1),
                                PaymentDate = !reader.IsDBNull(2) ? reader.GetInt32(2) : null,
                                Trip = new TripDTO
                                {
                                    IdTrip = reader.GetInt32(0),
                                    Name = reader.GetString(3),
                                    Description = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                    DateFrom = reader.GetDateTime(5),
                                    DateTo = reader.GetDateTime(6),
                                    MaxPeople = reader.GetInt32(7),
                                    Countries = new List<CountryDTO>()
                                }
                            };

                            clientTrips.Add(clientTrip);
                        }
                    }
                }
                
                foreach (var clientTrip in clientTrips)
                {
                    string countrySql = @"
                        SELECT c.IdCountry, c.Name
                        FROM Country c
                        JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry
                        WHERE ct.IdTrip = @IdTrip";

                    using (SqlCommand command = new SqlCommand(countrySql, connection))
                    {
                        command.Parameters.AddWithValue("@IdTrip", clientTrip.IdTrip);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var country = new CountryDTO
                                {
                                    IdCountry = reader.GetInt32(0),
                                    Name = reader.GetString(1)
                                };

                                clientTrip.Trip.Countries.Add(country);
                            }
                        }
                    }
                }
            }

            return clientTrips;
        }

        public async Task<bool> DoesClientExist(int clientId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT COUNT(1) FROM Client WHERE IdClient = @IdClient";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    int count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        public async Task<int> CreateClient(CreateClientRequest request)
        {
            if (string.IsNullOrEmpty(request.FirstName))
                throw new ArgumentException("FirstName is required");

            if (string.IsNullOrEmpty(request.LastName))
                throw new ArgumentException("LastName is required");

            if (string.IsNullOrEmpty(request.Email))
                throw new ArgumentException("Email is required");
            
            if (!IsValidEmail(request.Email))
                throw new ArgumentException("Invalid email format");
            
            if (!string.IsNullOrEmpty(request.Pesel) && !IsValidPesel(request.Pesel))
                throw new ArgumentException("Invalid PESEL format");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string checkEmailSql = "SELECT COUNT(1) FROM Client WHERE Email = @Email";
                using (SqlCommand command = new SqlCommand(checkEmailSql, connection))
                {
                    command.Parameters.AddWithValue("@Email", request.Email);
                    int emailExists = (int)await command.ExecuteScalarAsync();

                    if (emailExists > 0)
                    {
                        throw new InvalidOperationException("Email is already in use");
                    }
                }
                
                string insertSql = @"
                    INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                    VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);
                    SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@FirstName", request.FirstName);
                    command.Parameters.AddWithValue("@LastName", request.LastName);
                    command.Parameters.AddWithValue("@Email", request.Email);
                    command.Parameters.AddWithValue("@Telephone", (object)request.Telephone ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Pesel", (object)request.Pesel ?? DBNull.Value);
                    
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<bool> RegisterClientForTrip(int clientId, int tripId)
        {
            if (!await DoesClientExist(clientId))
                throw new ArgumentException($"Client with ID {clientId} not found");
            
            if (!await _tripsService.DoesTripExist(tripId))
                throw new ArgumentException($"Trip with ID {tripId} not found");
            
            if (await IsClientRegisteredForTrip(clientId, tripId))
                throw new InvalidOperationException("Client is already registered for this trip");
            
            if (await _tripsService.IsTripFull(tripId))
                throw new InvalidOperationException("Maximum number of participants has been reached for this trip");
            
            int registeredAt = int.Parse(DateTime.Now.ToString("yyyyMMdd"));

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string registerSql = @"
                    INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
                    VALUES (@IdClient, @IdTrip, @RegisteredAt, NULL)";

                using (SqlCommand command = new SqlCommand(registerSql, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    command.Parameters.AddWithValue("@IdTrip", tripId);
                    command.Parameters.AddWithValue("@RegisteredAt", registeredAt);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        public async Task<bool> RemoveClientFromTrip(int clientId, int tripId)
        {
            if (!await IsClientRegisteredForTrip(clientId, tripId))
                throw new ArgumentException("Client is not registered for this trip");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string deleteSql = @"
                    DELETE FROM Client_Trip 
                    WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

                using (SqlCommand command = new SqlCommand(deleteSql, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    command.Parameters.AddWithValue("@IdTrip", tripId);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        public async Task<bool> IsClientRegisteredForTrip(int clientId, int tripId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT COUNT(1) FROM Client_Trip 
                    WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    command.Parameters.AddWithValue("@IdTrip", tripId);
                    int count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }
        
        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private bool IsValidPesel(string pesel)
        {
            return Regex.IsMatch(pesel, @"^\d{11}$");
        }
    }
}