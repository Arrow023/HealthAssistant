namespace FitnessAgentsWeb.Models.ViewModels;

public class LoginViewModel
{
    public string? ErrorMessage { get; set; }
    public bool SsoEnabled { get; set; }
    public string SsoDisplayName { get; set; } = "SSO";
    public string SsoIcon { get; set; } = "fa-solid fa-arrow-right-to-bracket";
}
