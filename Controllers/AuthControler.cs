using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
//la dicitura using chat_ia.DatabaseConf; permette di utilizzare le classi presenti nel namespace DatabaseConf
using chat_ia.DatabaseConf;
using chat_ia.Models;


// Route ("[controller]) la rotta prende il nome della classe senza la dicitura Controller finale
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    //Ex: Questa rotta sara' quindi "auth/login"
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginModel model)
    {
        using (var context = new AppDbContext())
        {
            var user = context.Users.SingleOrDefault(u => u.Username == model.Username && u.Password == model.Password);
            if (user != null)
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes("your_very_long_secret_key_here_32_bytes_or_more");
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                    new Claim(ClaimTypes.Name, user.Username)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    Audience = "your_audience",
                    Issuer = "your_issuer",
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new { Token = tokenString });
            }
        }

        return Unauthorized();
    }

    // "auth/register"
    [HttpPost("register")]
    public IActionResult Register([FromBody] LoginModel model)
    {
        using (var context = new AppDbContext())
        {
            
            var exist = context.Users.SingleOrDefault(u => u.Username == model.Username);

            // Se l'utente non esiste si procede con il signin
            if (exist == null)
            {    // Crea un nuovo utente
                var newUser = new User
                {
                    Username = model.Username,
                    Password = model.Password // TODO: HASH
                };

                // Aggiungi l'utente al database
                context.Users.Add(newUser);
                //salva l'operazione
                context.SaveChanges();

                //processo generazione del token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes("your_very_long_secret_key_here_32_bytes_or_more");
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                    new Claim(ClaimTypes.Name, model.Username)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    Audience = "your_audience",
                    Issuer = "your_issuer",
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);
               
                return Ok(new { Token = tokenString });
            }
        }
        //altrimenti return Unauthorized
        return Unauthorized();
    }
}

public class LoginModel
{
    public string Username { get; set; }
    public string Password { get; set; }
}