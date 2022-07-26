using AuthNet.Configuration;
using AuthNet.Data;
using AuthNet.Models;
using AuthNet.Models.DTOs.Requests;
using AuthNet.Models.DTOs.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthNet.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthManagementController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuthSettings _authSettings;
        private readonly TokenValidationParameters _tokenValidationParams;
        private readonly AuthContext _authContext;
        private readonly IConfiguration _configuration;

        public AuthManagementController(UserManager<IdentityUser> userManager, IOptionsMonitor<AuthSettings> optionsMonitor, TokenValidationParameters tokenValidationParams, AuthContext authContext, IConfiguration configuration = null)
        {
            _userManager = userManager;
            _authSettings = optionsMonitor.CurrentValue;
            _tokenValidationParams = tokenValidationParams;
            _authContext = authContext;
            _configuration = configuration;
        }
        [HttpPost]
        [Route("Register")]
     
        public async Task<IActionResult> Register(/*[FromHeader]string authenticated,*/[FromBody] UserRegistrationDto user)
        {
            try
            {
                //if (authenticated!= _configuration.GetSection("AuthSettings")["Secret"])
                //{
                //    return Unauthorized("Você nao tem autorização para acessar este recurso");
                //}

                if (ModelState.IsValid)
                {
                    // We can utilise the model
                    var existingUser = await _userManager.FindByEmailAsync(user.Email);

                    if (existingUser != null)
                    {
                        return BadRequest(new RegistrationResponse()
                        {
                            Errors = new List<string>() {
                                "Email already in use"
                            },
                            Success = false
                        });
                    }

                    var newUser = new IdentityUser() { Email = user.Email, UserName = user.Username };
                    var isCreated = await _userManager.CreateAsync(newUser, user.Password);
                    if (isCreated.Succeeded)
                    {
                        var jwtToken = await GenerateJwtToken(newUser);

                        return Ok(jwtToken);
                    }
                    else
                    {
                        return BadRequest(new RegistrationResponse()
                        {
                            Errors = isCreated.Errors.Select(x => x.Description).ToList(),
                            Success = false
                        });
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            

            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>() {
                        "Invalid payload"
                    },
                Success = false
            });
        }

        private async Task<AuthResponse> GenerateJwtToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_authSettings.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),                   
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddSeconds(30), // 5-10 a Tempo de Expirar o token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken()
            {
                JwtId = token.Id,
                IsUsed = false,
                IsRevorked = false,
                UserId = user.Id,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                Token = RandomString(35) + Guid.NewGuid()
            };

            await _authContext.RefreshTokens.AddAsync(refreshToken);
            await _authContext.SaveChangesAsync();

            return new AuthResponse()
            {
                Token = jwtToken,
                Success = true,
                RefreshToken = refreshToken.Token
            };
        }

        private string RandomString(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(x => x[random.Next(x.Length)]).ToArray());
        }
    }
}
