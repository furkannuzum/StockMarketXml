// StockMarket.Api/Program.cs

using Microsoft.AspNetCore.Authentication.JwtBearer; // Authentication için
using Microsoft.IdentityModel.Tokens;                 // Token ayarları için
using Microsoft.OpenApi.Models;                     // Swagger/OpenAPI için
using StockMarket.Api.Services;                     // Servislerimiz için
using System.Text;                                  // Encoding için

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration; // IConfiguration'ı almak için

// CORS Politikası Adı
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins"; // Politikaya bir isim veriyoruz

// Add services to the container.

// CORS servisini ekle
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://127.0.0.1:5500",  // VS Code Live Server varsayılan portu
                                             "http://localhost:5500",   // Alternatif Live Server portu
                                             "null")                    // file:/// origin'i için (Geliştirme Amaçlı!)
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                          // .AllowCredentials(); // Eğer cookie veya credential'lar gönderiyorsan ve WithOrigins'de * yoksa
                      });
});

builder.Services.AddHttpClient(); // HttpClient'ı DI için kaydet
builder.Services.AddSingleton<IStockDataService, ExternalApiStockDataService>();
builder.Services.AddScoped<IUserRepository, InMemoryUserRepository>(); // UserRepository'yi DI'a ekle
builder.Services.AddSingleton<IUserPreferenceService, InMemoryUserPreferenceService>(); // <-- YENİ SERVİS KAYDI (Singleton veya Scoped olabilir)

builder.Services.AddControllers()
    .AddXmlSerializerFormatters() // XML formatında istek ve cevapları desteklemek için
    .AddNewtonsoftJson();
// JWT Authentication Yapılandırması
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    // options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});


// Swagger/OpenAPI servislerini ekle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Stock Market XML API",
        Description = "An ASP.NET Core Web API for providing stock market data in XML format.",
        Contact = new OpenApiContact
        {
            Name = "Furkan",
            Email = "furkan@example.com",
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT"),
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Lütfen geçerli bir JWT token girin. Örnek: \"Bearer {token}\"",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Stock Market API V1");
        // options.RoutePrefix = string.Empty;
    });
}

// app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();