using System.ComponentModel.DataAnnotations;

namespace Glitch.ViewModels
{
    public class PaymentViewModel
    {
        // Which game is being purchased
        public int GameId { get; set; }

        // Game info to display on payment page
        public string GameTitle { get; set; } = string.Empty;
        public string GameGenre { get; set; } = string.Empty;
        public decimal GamePrice { get; set; }
        public string? GameImage { get; set; }

        // User must enter the EXACT price to confirm payment
        // e.g. if price is $29.99, user types 29.99
        [Required(ErrorMessage = "Please enter the payment amount")]
        public decimal AmountEntered { get; set; }

        // User must enter their password to confirm purchase
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}