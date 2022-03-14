using BlazorMovie.Shared;
using MovieClient.Services;
using System.Net.Http.Json;

namespace MovieClient.Pages;

public partial class ResetPassword
{
    private EmailModel email = new();
    
    private async Task HandleValidSubmit()
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/Account/resetPassword", email);
        alertService.ShowAlert(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

}