using MinimalApi.DTOs;
using MinimalApi.Dominio.Entidades;

namespace MinimalApi.Dominio.Interfaces
{
    public interface IAdministradorServico
    {
        Administrador? Login(LoginDTO loginDTO);

        Administrador Incluir(Administrador adminstrador);

        List<Administrador> Todos(int? pagina);

        Administrador? BuscaPorId(int id);
    }
}