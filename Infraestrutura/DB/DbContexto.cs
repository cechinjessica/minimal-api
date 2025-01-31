using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Entidades;

namespace MinimalApi.Infraestrutura.Db;

public class DBContexto : DbContext
{
    private readonly IConfiguration _configuration;
    public DBContexto(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public DbSet<Administrador> Administradores { get; set; } = default!;
    public DbSet<Veiculo> Veiculos { get; set; } = default!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrador>().HasData(
            new Administrador { 
                Id = 1,
                Email = "administrador@teste.com", 
                Senha = "123456",
                Perfil = Dominio.Enuns.Perfil.Administrador.ToString()
            }
        );
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
        if (!optionsBuilder.IsConfigured)
        {
            var stringConexao = _configuration.GetConnectionString("mysql")?.ToString();
            if (!string.IsNullOrEmpty(stringConexao))
            {
                optionsBuilder.UseMySql(
                    stringConexao, 
                    ServerVersion.AutoDetect(stringConexao));
            }
        }
    }
}