using Domain.Constants;
using Domain.Entities;
using System.Security.Claims;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.AdminUsers;
using WebApi.Contracts.Common;

namespace WebApi.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminUsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminUsersController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var usersAll = new List<(string Id, string Email, string UserName, DateTime CreatedAtUtc, DateTimeOffset? LockoutEnd, bool EmailConfirmed)>();
        var rolePairsAll = new List<(string UserId, string RoleName)>();

        await using (var connection = _dbContext.Database.GetDbConnection())
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT Id, Email, UserName, CreatedAtUtc, LockoutEnd, EmailConfirmed
FROM AspNetUsers
WHERE IsDeleted = 0";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var id = reader.GetString(0);
                    var email = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    var userName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    var createdAt = reader.GetDateTime(3);
                    DateTimeOffset? lockoutEnd = reader.IsDBNull(4)
                        ? null
                        : reader.GetFieldValue<DateTimeOffset>(4);
                    var emailConfirmed = !reader.IsDBNull(5) && reader.GetBoolean(5);
                    usersAll.Add((id, email, userName, createdAt, lockoutEnd, emailConfirmed));
                }
            }

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT ur.UserId, r.Name
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var userId = reader.GetString(0);
                    var roleName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    rolePairsAll.Add((userId, roleName));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleExists = await _roleManager.RoleExistsAsync(role);
            if (!roleExists)
            {
                return Ok(new PagedResult<AdminUserDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    Items = Array.Empty<AdminUserDto>()
                });
            }
        }

        var filteredUsers = usersAll.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleUserIds = new HashSet<string>(
                rolePairsAll.Where(x => string.Equals(x.RoleName, role, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.UserId));
            filteredUsers = filteredUsers.Where(x => roleUserIds.Contains(x.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filteredUsers = filteredUsers.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Email) && x.Email.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.UserName) && x.UserName.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var totalCount = filteredUsers.Count();
        var users = filteredUsers
            .OrderBy(x => x.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var userIdSet = new HashSet<string>(users.Select(x => x.Id));
        var rolesLookup = rolePairsAll
            .Where(x => userIdSet.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.Select(r => r.RoleName).ToList());

        var items = users.Select(user => new AdminUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = rolesLookup.TryGetValue(user.Id, out var roles)
                ? roles
                : Array.Empty<string>(),
            CreatedAtUtc = user.CreatedAtUtc,
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
            EmailConfirmed = user.EmailConfirmed
        }).ToList();

        return Ok(new PagedResult<AdminUserDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpGet("/admin/roles")]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles()
    {
        var roles = await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RoleDto { Name = x.Name ?? string.Empty })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequest request)
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

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(IdentityErrors(roleResult)));
        }

        if (request.Role == RoleNames.Expert)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { nameof(request.FullName), new[] { "Full name is required for experts." } }
                }));
            }

            _dbContext.ExpertProfiles.Add(new ExpertProfile
            {
                UserId = user.Id,
                FullName = request.FullName.Trim(),
                IsApproved = true
            });
        }
        else if (request.Role == RoleNames.Company)
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName))
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { nameof(request.CompanyName), new[] { "Company name is required for companies." } }
                }));
            }

            _dbContext.CompanyProfiles.Add(new CompanyProfile
            {
                UserId = user.Id,
                CompanyName = request.CompanyName.Trim()
            });
        }

        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateUserRole([FromRoute] string id, [FromBody] UpdateUserRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!removeResult.Succeeded)
            {
                return ValidationProblem(new ValidationProblemDetails(IdentityErrors(removeResult)));
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(IdentityErrors(addResult)));
        }

        if (request.Role == RoleNames.Expert)
        {
            var profile = await _dbContext.ExpertProfiles
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (profile is null)
            {
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                    {
                        { nameof(request.FullName), new[] { "Full name is required for experts." } }
                    }));
                }

                profile = new ExpertProfile
                {
                    UserId = user.Id,
                    FullName = request.FullName.Trim(),
                    IsApproved = true
                };
                _dbContext.ExpertProfiles.Add(profile);
            }
            else if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                profile.FullName = request.FullName.Trim();
            }

            profile.IsApproved = true;
        }
        else if (request.Role == RoleNames.Company)
        {
            var profile = await _dbContext.CompanyProfiles
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (profile is null)
            {
                if (string.IsNullOrWhiteSpace(request.CompanyName))
                {
                    return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                    {
                        { nameof(request.CompanyName), new[] { "Company name is required for companies." } }
                    }));
                }

                profile = new CompanyProfile
                {
                    UserId = user.Id,
                    CompanyName = request.CompanyName.Trim()
                };
                _dbContext.CompanyProfiles.Add(profile);
            }
            else if (!string.IsNullOrWhiteSpace(request.CompanyName))
            {
                profile.CompanyName = request.CompanyName.Trim();
            }
        }

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/lock")]
    public async Task<IActionResult> LockUser([FromRoute] string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(currentUserId) && currentUserId == id)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(id), new[] { "You cannot lock your own account." } }
            }));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(IdentityErrors(result)));
        }

        return NoContent();
    }

    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> UnlockUser([FromRoute] string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(IdentityErrors(result)));
        }

        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword([FromRoute] string id, [FromBody] ResetUserPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.Password);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(IdentityErrors(result)));
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser([FromRoute] string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(currentUserId) && currentUserId == id)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(id), new[] { "You cannot delete your own account." } }
            }));
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        user.IsDeleted = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static Dictionary<string, string[]> IdentityErrors(IdentityResult result)
    {
        return new Dictionary<string, string[]>
        {
            { "Identity", result.Errors.Select(e => e.Description).ToArray() }
        };
    }
}
