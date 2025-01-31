using MinimalApi.Dominio.Enuns;

namespace Dominio.ModelViews
{
    public record AdministradorLogadoModelView
    {

        public string Email { get; set; }

        public string Perfil { get; set; }
        public string Token { get; set; }
    }
}