using Microsoft.AspNetCore.Mvc;

namespace STOREBOOKS.Models
{
    public class ResetPasswordViewModel
    {
        public string Token { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

}
