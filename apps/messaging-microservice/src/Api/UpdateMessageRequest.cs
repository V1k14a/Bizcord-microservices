using System.ComponentModel.DataAnnotations;

namespace MessagingMicroservice.Api;

public class UpdateMessageRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;
}
