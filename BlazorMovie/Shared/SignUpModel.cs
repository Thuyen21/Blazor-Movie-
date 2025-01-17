﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlazorMovie.Shared;

public class SignUpModel
{
    [Required]
    [DataType(DataType.EmailAddress)]
    [DisplayName("Email")]
    [EmailAddress]

    public string? Email { get; set; }

    [Required]
    [DisplayName("Password")]
    [DataType(DataType.Password)]
    [StringLength(50, MinimumLength = 7)]
    [PasswordPropertyText]
    public string? Password { get; set; }

    [Required]
    [DisplayName("ConfirmPassword")]
    [DataType(DataType.Password)]
    [StringLength(50, MinimumLength = 7)]
    [PasswordPropertyText]
    public string? ConfirmPassword { get; set; }


    [Required]
    [DisplayName("Name")]
    [DataType(DataType.Text)]
    public string? Name { get; set; }

    [Required]
    [DisplayName("Date of birth")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    public string? Role { get; set; }
}

