using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IClientsService _clientsService;

        public ClientsController(IClientsService clientsService)
        {
            _clientsService = clientsService;
        }

        /// <summary>
        /// GET /api/clients/{id}/trips - Retrieves all trips associated with a specific client
        /// </summary>
        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            try
            {
                if (!await _clientsService.DoesClientExist(id))
                {
                    return NotFound($"Client with ID {id} not found");
                }

                var clientTrips = await _clientsService.GetClientTrips(id);
                
                if (clientTrips.Count == 0)
                {
                    return Ok(new { Message = $"Client with ID {id} has no registered trips" });
                }

                return Ok(clientTrips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// POST /api/clients - Creates a new client record
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
        {
            try
            {
                int newClientId = await _clientsService.CreateClient(request);
                return StatusCode(201, new { IdClient = newClientId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// PUT /api/clients/{id}/trips/{tripId} - Registers a client for a specific trip
        /// </summary>
        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
        {
            try
            {
                await _clientsService.RegisterClientForTrip(id, tripId);
                
                return Ok(new {
                    Message = "Client successfully registered for the trip",
                    RegisteredAt = int.Parse(DateTime.Now.ToString("yyyyMMdd"))
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("already registered"))
                    return Conflict(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// DELETE /api/clients/{id}/trips/{tripId} - Removes a client's registration from a trip
        /// </summary>
        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> RemoveClientFromTrip(int id, int tripId)
        {
            try
            {
                await _clientsService.RemoveClientFromTrip(id, tripId);
                return Ok(new { Message = "Client registration successfully removed from the trip" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}