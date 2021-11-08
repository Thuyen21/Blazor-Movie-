﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlazorMovie.Shared;

public class LogInModel
{
    [Required]
    [EmailAddress]
    [DisplayName("Email")]

#pragma warning disable CS8618 // Non-nullable property 'Email' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
    public string Email { get; set; }
#pragma warning restore CS8618 // Non-nullable property 'Email' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.

    [Required]
    [DisplayName("Password")]
    [StringLength(50, MinimumLength = 7)]
    [PasswordPropertyText(false)]
#pragma warning disable CS8618 // Non-nullable property 'Password' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
    public string Password { get; set; }
#pragma warning restore CS8618 // Non-nullable property 'Password' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.


    public string? UserAgent { get; set; }
}