﻿using Microsoft.AspNetCore.Components.Authorization;

namespace MovieClient.Services;

public class AccountService : IAccountService
{
    private readonly AuthenticationStateProvider authenticationStateProvider;
    public AccountService(AuthenticationStateProvider authenticationStateProvider)
    {
        this.authenticationStateProvider = authenticationStateProvider;
    }
    public void checkAuthentication()
    {
        (authenticationStateProvider as CustomAuthenticationStateProvider).Update();
    }
}


