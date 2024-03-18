global using Frameworks.Aspnetcore.Library.Extensions;
global using Frameworks.Aspnetcore.Library.MIddleware;

global using MediatR;
global using Asp.Versioning;
global using FluentResults;
global using FluentResults.Extensions;
global using Microsoft.AspNetCore.Mvc;

global using FluentValidation;

global using Microsoft.Extensions.Logging;

global using Ardalis;
global using Ardalis.GuardClauses;

global using Models.Shared.Response;
global using System.Net;

global using System.Text.RegularExpressions;

global using Microsoft.Data.SqlClient;
global using Microsoft.Extensions.Caching.Distributed;
global using Models.Shared.Enums;
global using System.Data.SqlTypes;

global using Hangfire;

global using Microsoft.AspNetCore.RateLimiting;
global using Newtonsoft.Json;

global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc.Infrastructure;

global using Microsoft.EntityFrameworkCore;

global using Users.Application.Modules.Infrastructures;

global using Users.Application.Modules.Shared.BaseController;
global using Users.Contracts.Features;

global using HashPassword;

global using Users.Application.Modules.Infrastructures.Entity;
global using Users.Contracts.Shared;

global using Users.Application.Modules.Shared.Repository;

global using MassTransit;

global using Notification.Contracts.Features;
global using Notifications.Application.Modules.Features;
global using Organization.Contracts.Features;
global using System.Threading;
global using Users.Application.Modules.Shared.Cache;

global using Users.Application.Modules.Shared.Services;
global using Utility.Shared.Cache;

global using Users.Contracts.Features.ForgetPassword;

global using Microsoft.AspNetCore.Http;
global using System.Security.Claims;
global using Users.Contracts.Shared.Services;

global using Microsoft.Extensions.Options;
global using Users.Contracts.Shared.Enums;