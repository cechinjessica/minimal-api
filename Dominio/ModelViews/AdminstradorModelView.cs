using MinimalApi.Dominio.Enuns;

namespace Dominio.ModelViews
{
    public record AdministradorModelView
    {

        public string Email { get; set; }
        public int Id { get; set; }

        public string Perfil { get; set; }
    }
}