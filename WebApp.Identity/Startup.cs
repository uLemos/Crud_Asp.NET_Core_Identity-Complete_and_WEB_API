using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebApp.Identity
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddControllersWithViews();

            //services.AddRazorPages();

            var connectionString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=Identity;Data Source=DESKTOP-AM9S57K\SQLEXPRESS";
            var migrationAssembly = typeof(Startup) //Variável criada para evitar o erro de assembly no cmd.
                .GetTypeInfo().Assembly
                .GetName().Name;

            services.AddDbContext<MyUserDbContext>(
                opt => opt.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(migrationAssembly)) //Lambda usada para que seja realizado uma migrationAssembly para cada consulta no sql.
            );

            services.AddHealthChecks();
            services.AddMvc();

            services.AddIdentity<MyUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;

                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 4;

                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.AllowedForNewUsers = true;

            }) //Serviço para utilizar o User com IdentityCore.
            .AddEntityFrameworkStores<MyUserDbContext>() //Utilizando o AddIdentity sem ser com o Core, para poder passar como tipo, o IdentityRole, que é um identitficador de permissão
            .AddDefaultTokenProviders() //Provedor de token padrão
            .AddPasswordValidator<NaoContemValidadorSenha<MyUser>>();

            services.Configure<DataProtectionTokenProviderOptions>(
                options => options.TokenLifespan = TimeSpan.FromHours(3) //Quantidade de tempo no qual o token irá durar.
                );

            services.ConfigureApplicationCookie (options => //Traça uma rota padrão para o Cookie, sendo nomeado como "Identity.Application" nomeamento Default.
                options.LoginPath = "/Home/Login"
                );

            services.AddScoped<IUserClaimsPrincipalFactory<MyUser>, MyUserClaimsPrincipalFactory>();
            //Especificando que, toda vez 
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment  env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseAuthentication(); //Usar o app sempre quando for adicionar um service, no caso, é no contexto de Authentication.

            app.UseStaticFiles();

            //app.UseRouting();

            //app.UseAuthorization();

            app.UseMvc(endpoints =>
            {
                endpoints.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
