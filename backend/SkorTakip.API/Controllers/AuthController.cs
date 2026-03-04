using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SkorTakip.API.DTOs;
using SkorTakip.API.Models;
using SkorTakip.API.Services;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtService jwtService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new ApplicationUser
        {
            UserName = dto.UserName ?? dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        var token = _jwtService.GenerateToken(user);
        var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "60");

        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized(new { message = "Email veya şifre hatalı" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Email veya şifre hatalı" });

        var token = _jwtService.GenerateToken(user);
        var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "60");

        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        });
    }
}
