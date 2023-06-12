using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebAPI.Dominio
{
    public class UserRole : IdentityUserRole<int> //relacionamento entre user e role 
    {// UserID e RoleID precisam de um tipo para realizar o relacionamento.
        public User User { get; set; }
        public Role Role { get; set; }
    }
}
