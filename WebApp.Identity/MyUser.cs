using Microsoft.AspNetCore.Identity;

namespace WebApp.Identity
{
    public class MyUser : IdentityUser //Ao herdar de Identity, é possível criar no banco todas as tabelas padrões existentes.
    {
        public string NomeCompleto { get; set; } // NomeCompleto é uma tabela que está sendo inserida de maneira manual
        public string Member { get; set; } = "Member";
        public string OrgId { get; set; }
    }

    public class Organization
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
