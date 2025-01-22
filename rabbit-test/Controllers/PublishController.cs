using Common.Dtos;
using Microsoft.AspNetCore.Mvc;
using rabbit_test.Services;

namespace rabbit_test.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublishController : ControllerBase
    {
        private readonly RabbitMqPublisher _publisher;

        public PublishController(RabbitMqPublisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost]
        public IActionResult PublishMessage([FromBody] MessageDto message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Content))
                return BadRequest("Invalid message");

            message.Id = Guid.NewGuid();
            message.CreatedAt = DateTime.UtcNow;

            _publisher.Publish(message);

            return Ok(new { Status = "Message published", message.Id });
        }
    }
}
