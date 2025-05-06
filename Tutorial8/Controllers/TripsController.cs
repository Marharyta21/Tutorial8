using Microsoft.AspNetCore.Mvc;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        /// <summary>
        /// GET /api/trips - Retrieves all available trips with their basic information
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            try
            {
                var trips = await _tripsService.GetTrips();
                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}