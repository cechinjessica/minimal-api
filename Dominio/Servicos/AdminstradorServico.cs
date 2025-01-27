using System.Data.Common;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;

namespace MinimalApi.Dominio.Servicos
{
    public class AdministradorServico : IAdministradorServico
    {
        private readonly DBContexto _contexto;
        public AdministradorServico(DBContexto contexto)
        {
            _contexto = contexto;
        }

        public Administrador? Login(LoginDTO loginDTO){
            return _contexto.Administradores.Where(a=> a.Email == loginDTO.Email && a.Senha == loginDTO.Senha).FirstOrDefault();    
        }
    }
}