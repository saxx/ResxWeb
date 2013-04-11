using System.ComponentModel.DataAnnotations;

namespace ResxWeb.Models.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required (ErrorMessage = "Enter your username.")]
        [Display(Name = "Username:")]
        public string UserName { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password:")]
        public string Password { get; set; }
    }
}