using Dominio.ModelViews;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enuns;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Servicos;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;

# region builder
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if (string.IsNullOrEmpty(key))
    key = "123456";

builder.Services.AddAuthentication(option => {
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option => {
    option.TokenValidationParameters = new TokenValidationParameters{
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key))
    };
});
builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme{
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT aqui:"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement{
        {
            new OpenApiSecurityScheme{
                Reference = new OpenApiReference{
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddDbContext<DBContexto>(options => {
    options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
        );
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();
# endregion

# region Home 
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
# endregion

# region Administradores
string GerarTokenJwt(Administrador administrador){
    if (string.IsNullOrEmpty(key) )
        return string.Empty;
   
    var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>{
            new Claim("Email", administrador.Email),
            new Claim("Perfil", administrador.Perfil),
            new Claim(ClaimTypes.Role, administrador.Perfil)    
        };

    var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: credentials
        );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.MapPost("/adminstradores/login", ([FromBody]LoginDTO loginDTO, IAdministradorServico administradorServico) => {
    var adm = administradorServico.Login(loginDTO);
    if (adm != null){
        string token = GerarTokenJwt(adm);
        return Results.Ok(new AdministradorLogadoModelView{
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token
        });
    }
    else
        return Results.Unauthorized();
}).AllowAnonymous().WithTags("Administradores");

app.MapPost("/adminstradores", ([FromBody]AdministradorDTO adminstradorDTO, IAdministradorServico administradorServico) => {
    var validacao = new ErrosDeValidacao{
        Mensagens = new List<string>()
    };

    if (string.IsNullOrEmpty(adminstradorDTO.Senha))
        validacao.Mensagens.Add("Senha não pode ser vazio");

    if (string.IsNullOrEmpty(adminstradorDTO.Email))
        validacao.Mensagens.Add("Email não pode ser vazio");
    
    if (adminstradorDTO.Perfil == null)
        validacao.Mensagens.Add("Perfil não pode ser vazio");
    
    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);
    
    var administrador = new Administrador{
        Email = adminstradorDTO.Email,
        Senha = adminstradorDTO.Senha,
        Perfil = adminstradorDTO.Perfil.ToString()
    };

    administradorServico.Incluir(administrador);

    return Results.Created($"/adminstradores/{administrador.Id}", new AdministradorModelView{
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil
    });

}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrador"})
  .WithTags("Administradores");

app.MapGet("/adminstradores/", ([FromQuery]int? pagina, IAdministradorServico administradorServico) => {
    var adms = new List<AdministradorModelView>();
    var administradores = administradorServico.Todos(pagina);
    foreach (var adm in administradores)
    {
        Perfil perfil;
        if (!Enum.TryParse<Perfil>(adm.Perfil, out perfil))
            perfil = Perfil.Editor; 
        
        adms.Add(new AdministradorModelView{
            Id = adm.Id,
            Email = adm.Email,
            Perfil = perfil.ToString()
        });
    }

    return Results.Ok(adms);
})//.RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrador"})
  .WithTags("Administradores");

app.MapGet("/adminstradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) => {
    var administrador = administradorServico.BuscaPorId(id);

    if (administrador == null)
        return Results.NotFound();

    var adm = new AdministradorModelView{
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil
    };

    return Results.Ok(adm); 
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrador"})
  .WithTags("Administradores");

# endregion

# region Veiculos
 ErrosDeValidacao ValidaDTO(VeiculoDTO veiculoDTO){
    var validacao = new ErrosDeValidacao{
        Mensagens = new List<string>()
    };

    if (string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao.Mensagens.Add("Nome não pode ser vazio");

    if (string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao.Mensagens.Add("Marca não pode ser vazio");

    if (veiculoDTO.Ano < 1900)
        validacao.Mensagens.Add("Ano não pode ser menor que 1900");

    return validacao;
}

app.MapPost("/veiculos", ([FromBody]VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => {
    var validacao = new ErrosDeValidacao();
    validacao = ValidaDTO(veiculoDTO);

    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    var veiculo = new Veiculo{
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
        };
    
    veiculoServico.Incluir(veiculo);
    
    return Results.Created($"/veiculos/{veiculo.Id}", veiculo); 
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrador, Editor"})
  .WithTags("Veiculos");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) => {
    var veiculos = veiculoServico.Todos(pagina);

    return Results.Ok(veiculos); 
}).RequireAuthorization().WithTags("Veiculos");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) => {
    var veiculo = veiculoServico.BuscaPorId(id);

    if (veiculo == null)
        return Results.NotFound();

    return Results.Ok(veiculo); 
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrador, Editor"})
  .WithTags("Veiculos");

app.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO ,IVeiculoServico veiculoServico) => {
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo == null)
        return Results.NotFound();
    
    var validacao = new ErrosDeValidacao();
    validacao = ValidaDTO(veiculoDTO);
    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculoServico.Atualizar(veiculo);

    return Results.Ok(veiculo); 
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Administrador"})
.WithTags("Veiculos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) => {
    var veiculo = veiculoServico.BuscaPorId(id);

    if (veiculo == null)
        return Results.NotFound();

    veiculoServico.Apagar(veiculo);

    return Results.NoContent(); 
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrador"})
  .WithTags("Veiculos");

# endregion

# region app
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");


app.Run();
# endregion
