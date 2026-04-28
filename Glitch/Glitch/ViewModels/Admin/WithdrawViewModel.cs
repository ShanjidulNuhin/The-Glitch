using System.ComponentModel.DataAnnotations;

namespace Glitch.ViewModels.Admin
{
    public class WithdrawViewModel
    {
        public decimal AvailableBalance { get; set; }
        public decimal WithdrawableBalance { get; set; }

        [Required(ErrorMessage = "Please enter an amount to withdraw.")]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be at least $1.")]
        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = "Bank";
        public DateTime? DateOfBirth { get; set; }
    }
}
