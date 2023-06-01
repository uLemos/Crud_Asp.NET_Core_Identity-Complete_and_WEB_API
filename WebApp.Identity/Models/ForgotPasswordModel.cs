using System.ComponentModel.DataAnnotations;

namespace WebApp.Identity.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
