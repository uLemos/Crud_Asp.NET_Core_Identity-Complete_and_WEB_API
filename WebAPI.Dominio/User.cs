using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace WebAPI.Dominio
{
    public class User : IdentityUser<int> //Ao herdar de Identity, é possível criar no banco todas as tabelas padrões existentes.
    {
        public string NomeCompleto { get; set; } // NomeCompleto é uma tabela que está sendo inserida de maneira manual
        public string Member { get; set; } = "Member";
        public string OrgId { get; set; }
        public List<UserRole> UserRoles { get; set; } //Lista de UserRoles
    }
}
