﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Assignment1.Data;
using FileData;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Models;

namespace Assignment1.Authorization
{
    public class CustomAuthentication : AuthenticationStateProvider
    {
        private readonly IJSRuntime jsRuntime;
        private readonly IUserService userService;
        private User cachedUser;

        public CustomAuthentication(IJSRuntime jsRuntime, IUserService userService)
        {
            this.jsRuntime = jsRuntime;
            this.userService = userService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity();
            if (cachedUser == null)
            {
                string userAsJson = await jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "currentUser");
                if (!string.IsNullOrEmpty(userAsJson))
                {
                    User tmp = JsonSerializer.Deserialize<User>(userAsJson);
                    ValidateLogin(tmp.username, tmp.password);
                }
            }
            else
            {
                identity = SetupClaimsForUser(cachedUser);
            }

            ClaimsPrincipal cachedClaimsPrincipal = new ClaimsPrincipal(identity);
            return await Task.FromResult(new AuthenticationState(cachedClaimsPrincipal));
        }

        public void ValidateLogin(string username, string password)
        {
            Console.WriteLine("Validating log in");
            if (string.IsNullOrEmpty(username)) throw new Exception("Enter username");
            if (string.IsNullOrEmpty(password)) throw new Exception("Enter password");
            ClaimsIdentity identity = new ClaimsIdentity();
            try
            {
                User user = UserServiceImpl.ValidateUser(username, password);
                identity = SetupClaimsForUser(user);
                string serialisedData = JsonSerializer.Serialize(user);
                jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "currentUser", serialisedData);
                cachedUser = user;
            }
            catch (Exception e)
            {
                throw new Exception("Identity claim failed");
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
        }

        public async Task Logout()
        {
            cachedUser = null;
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            await jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "currentUser", "");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        private ClaimsIdentity SetupClaimsForUser(User user)
        {
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.username));
            claims.Add(new Claim("Level", user.SecurityLevel));
            
            ClaimsIdentity identity = new ClaimsIdentity(claims, "apiauth_type");
            return identity;
        }
    }
}
