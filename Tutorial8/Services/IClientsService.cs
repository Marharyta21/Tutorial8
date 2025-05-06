using Tutorial8.Models.DTOs;

namespace Tutorial8.Services
{
    public interface IClientsService
    {
        Task<List<ClientTripDTO>> GetClientTrips(int clientId);
        Task<bool> DoesClientExist(int clientId);
        Task<int> CreateClient(CreateClientRequest client);
        Task<bool> RegisterClientForTrip(int clientId, int tripId);
        Task<bool> RemoveClientFromTrip(int clientId, int tripId);
        Task<bool> IsClientRegisteredForTrip(int clientId, int tripId);
    }
}