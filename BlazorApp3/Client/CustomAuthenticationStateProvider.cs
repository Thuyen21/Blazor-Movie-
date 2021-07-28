﻿using BlazorApp3.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BlazorApp3.Client
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;

        public CustomAuthenticationStateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = await _httpClient.GetFromJsonAsync<AccountManagementModel>("user/GetCurrentUser");
            
            if (user.Email != null)
            {

                var claimsIdentity = new ClaimsIdentity(new[] {


                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Email, user.Email) 
                }, "serverAuth");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                return new AuthenticationState(claimsPrincipal);
            }

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        }
        
    }
}