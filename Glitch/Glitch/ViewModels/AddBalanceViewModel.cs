using System.ComponentModel.DataAnnotations;

namespace Glitch.ViewModels
{
    public class AddBalanceViewModel
    {
        [Required(ErrorMessage = "Please enter an amount")]
        [Range(1, 10000, ErrorMessage = "Amount must be between $1 and $10,000")]
        public decimal Amount { get; set; }

        public string? PaymentMethod { get; set; } // Bank, Card, Mobile
        public DateTime? DateOfBirth { get; set; } // For saving card DOB
    }
}
