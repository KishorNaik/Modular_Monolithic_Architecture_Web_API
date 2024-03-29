﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Users.Application.Modules.Infrastructures.Entity;

public partial class Tuser
{
    public decimal Id { get; set; }

    public Guid Identifier { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string EmailId { get; set; }

    public string MobileNo { get; set; }

    public int UserType { get; set; }

    public string Salt { get; set; }

    public string Hash { get; set; }

    public bool Status { get; set; }

    public bool? IsEmailVerified { get; set; }

    public Guid? EmailToken { get; set; }

    public string RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public Guid? PasswordResetToken { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public byte[] Version { get; set; }
}