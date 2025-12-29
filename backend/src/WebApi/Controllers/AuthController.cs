using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApi.Configuration;
using WebApi.Contracts.Auth.Requests;
using WebApi.Contracts.Auth.Responses;

namespace WebApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("register/expert")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterExpert([FromBody] RegisterExpertRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(request.Email), new[] { "Email is already registered." } }
            }));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(IdentityErrors(createResult)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, RoleNames.Expert);
        if (!roleResult.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(IdentityErrors(roleResult)));
        }

        var profile = new ExpertProfile
        {
            UserId = user.Id,
            FullName = request.FullName,
            IsApproved = false
        };

        _dbContext.ExpertProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("register/company")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterCompany([FromBody] RegisterCompanyRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(request.Email), new[] { "Email is already registered." } }
            }));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(IdentityErrors(createResult)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, RoleNames.Company);
        if (!roleResult.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(IdentityErrors(roleResult)));
        }

        var profile = new CompanyProfile
        {
            UserId = user.Id,
            CompanyName = request.CompanyName
        };

        _dbContext.CompanyProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        if (role == RoleNames.Expert)
        {
            var profile = await _dbContext.ExpertProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile is null || !profile.IsApproved)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Expert approval required.",
                    Detail = "Your account is pending approval.",
                    Status = StatusCodes.Status403Forbidden
                });
            }
        }

        var token = GenerateToken(user, roles);

        return Ok(new AuthResponse
        {
            AccessToken = token,
            Role = role,
            Email = user.Email ?? request.Email
        });
    }

    [HttpGet("/me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        var response = new MeResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = role
        };

        if (role == RoleNames.Expert)
        {
            var profile = await _dbContext.ExpertProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            response.FullName = profile?.FullName;
            response.ExpertApproved = profile?.IsApproved;
        }
        else if (role == RoleNames.Company)
        {
            var profile = await _dbContext.CompanyProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            response.CompanyName = profile?.CompanyName;
        }

        return Ok(response);
    }

    private string GenerateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static Dictionary<string, string[]> IdentityErrors(IdentityResult result)
    {
        return new Dictionary<string, string[]>
        {
            { "Identity", result.Errors.Select(e => e.Description).ToArray() }
        };
    }
}
