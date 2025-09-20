using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using FashionStore.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FashionStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    public AuthController(AppDbContext db, IConfiguration cfg){ _db=db; _cfg=cfg; }

    public record RegisterDto(string Email, string Password, string? FullName, string? Phone, string? Address);
    public record LoginDto(string Email, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if(await _db.Users.AnyAsync(x => x.Email == dto.Email))
            return BadRequest("Email đã tồn tại.");

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User {
            Email = dto.Email,
            MatKhauHash = hash,
            HoTen = dto.FullName,
            SoDienThoai = dto.Phone,
            DiaChi = dto.Address,
            NgayTao = DateTime.UtcNow,
            VaiTroId = await _db.Roles.Where(r=>r.TenVaiTro=="Khách hàng").Select(r=>r.VaiTroId).FirstOrDefaultAsync() switch {
                0 => 1, // fallback nếu chưa seed role
                var x => x
            }
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { user.NguoiDungId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
        if (u == null || !BCrypt.Net.BCrypt.Verify(dto.Password, u.MatKhauHash))
            return Unauthorized("Sai email hoặc mật khẩu.");

        var claims = new List<Claim> {
            new Claim(JwtRegisteredClaimNames.Sub, u.NguoiDungId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, u.Email),
            new Claim(ClaimTypes.NameIdentifier, u.NguoiDungId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"], audience: _cfg["Jwt:Audience"],
            claims: claims, expires: DateTime.UtcNow.AddHours(int.Parse(_cfg["Jwt:ExpiresHours"]!)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}
