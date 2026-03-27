using System.ComponentModel.DataAnnotations;

namespace MessagingMicroservice.Api;

public class CreateMessageRequest
{
    [Required]
    public Guid ChannelId { get; set; }

    [Required]
    public Guid AuthorId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;
}
