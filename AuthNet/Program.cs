using AuthNet.Middleware.RateLimiting;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using AuthNet.Data;
using AuthNet.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

///Taxa de Limitações
builder.Services.AddRateLimiting(builder.Configuration);
///Conifugrar Key token
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));

///Banco de Dados
builder.Services.AddDbContext<AuthContext>(options =>
           options.UseSqlite(
               builder.Configuration.GetConnectionString("DefaultConnection")
           ));
///Configuraçao do JWT
var key = Encoding.ASCII.GetBytes(builder.Configuration["AuthSettings:Secret"]);

var tokenValidationParams = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ValidateIssuer = false,
    ValidateAudience = false,
    ValidateLifetime = true,
    RequireExpirationTime = false,
    ClockSkew = TimeSpan.Zero
};

builder.Services.AddSingleton(tokenValidationParams);
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(jwt => {
    jwt.SaveToken = true;
    jwt.TokenValidationParameters = tokenValidationParams;
});
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<AuthContext>();



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(auth =>
{
    auth.SwaggerDoc("v1", new OpenApiInfo { Title = "TodoApp", Version = "v1" });
    auth.AddSecurityDefinition("BearerAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
        In = ParameterLocation.Header,
        Name = "Authorization",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    //c.OperationFilter<AuthResponsesOperationFilter>();
});



///CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Open", builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(auth => auth.SwaggerEndpoint("/swagger/v1/swagger.json", "API Auth Web v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseAuthorization();
app.UseCors("Open");

app.MapControllers();

app.Run();
