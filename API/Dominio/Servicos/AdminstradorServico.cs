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

        public Administrador Incluir(Administrador adminstrador){
            var administrador = new Administrador{
                Email = adminstrador.Email,
                Senha = adminstrador.Senha,
                Perfil = adminstrador.Perfil.ToString()
            };

            _contexto.Administradores.Add(administrador);
            _contexto.SaveChanges();

            return administrador;
        }

       public List<Administrador> Todos(int? pagina = 1)
        {
            int itensPorPagina = 10;
            var query = _contexto.Administradores.AsQueryable();

            if (pagina.HasValue)
                query = query.Skip(((int)pagina - 1) * itensPorPagina).Take(itensPorPagina);
            
            return query.ToList();
            
        }

        public Administrador? BuscaPorId(int id)
        {
            return _contexto.Administradores.Find(id);
        }
    }
}