using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WebApp.Identity.Models;

namespace WebApp.Identity.Controllers
{
    //[ApiController] -> Com Razor não é necessário utilizar API
    public class HomeController : Controller
    {
        private readonly UserManager<MyUser> _userManager;
        private readonly IUserClaimsPrincipalFactory<MyUser> _userClaimsPrincipalFactory;
        private readonly SignInManager<MyUser> _signInManager;

        public HomeController(UserManager<MyUser> userManager,
            IUserClaimsPrincipalFactory<MyUser> userClaimsPrincipalFactory,
            SignInManager<MyUser> signInManager)
        {
            _userManager = userManager;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        { 
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user != null && !await _userManager.IsLockedOutAsync(user))//IsLockedOut verifica se está bloqueado o user. OU SEJA, se o usuário errar a senha, n entra no IF.
                {// Se o Usuário errar 3 vezes, o usuário é bloqueado.
                    if (await _userManager.CheckPasswordAsync(user, model.Password))
                    {
                        //var identity = new ClaimsIdentity("Identity.Application"); //Adicionando os cookies via Claims
                        //identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id)); -> MÉTODO ANTIGO PARA USAR COOKIES VIA CLAIM
                        //identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
                        
                        if (!await _userManager.IsEmailConfirmedAsync(user))
                        {
                            ModelState.AddModelError("", "E-mail não está válido!");
                            return View();
                        }

                        await _userManager.ResetAccessFailedCountAsync(user); //Pra caso a pessoa erre o login.

                        if (await _userManager.GetTwoFactorEnabledAsync(user))
                        {
                            var validator = await _userManager.GetValidTwoFactorProvidersAsync(user);

                            if (validator.Contains("Email"))
                            {
                                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email"); //Cria o Token
                                System.IO.File.WriteAllText("email2sv.txt", token); //Passa o token em um arquivo txt.

                                await HttpContext.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, Store2FA(user.Id, "Email")); //Faz a validação

                                return RedirectToAction("TwoFactor"); //Envia pra action TwoFactor.
                            }
                        }

                        var principal = await _userClaimsPrincipalFactory.CreateAsync(user); // -> MÉTODO SIMPLIFICADO USANDO O "IUserClaimsPrincipalFactory".

                        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

                        //var signInResult = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false); //signIn economiza muito código e ainda possui a flexibilidade de novas funcionalidades no Login.

                        //if (signInResult.Succeeded)
                        //{
                        return RedirectToAction("About");
                    }

                    await _userManager.AccessFailedAsync(user);

                    if (await _userManager.IsLockedOutAsync(user))
                    {
                        //Email deve ser enviado com sugestão de alteração de senha!
                    }
                }

                ModelState.AddModelError("", "Usuário ou Senha Inválida!");
            }
            return View();
        }

        public ClaimsPrincipal Store2FA(string userId, string provider)
        {
            var identity = new ClaimsIdentity(new List<Claim>
            {
                new Claim("sub", userId),
                new Claim("amr", provider)
            }, IdentityConstants.TwoFactorUserIdScheme);

            return new ClaimsPrincipal(identity);
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ConfirmEmailAdress(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    return View("Success");
                }
            }
            return View("Error");
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetURL = Url.Action("ResetPassword", "Home",
                        new { token = token, email = model.Email }, Request.Scheme);

                    //Neste momento que pode ser usado qualquer forma de envio de e-mail ao usuário.

                    System.IO.File.WriteAllText("resetLink.txt", resetURL);

                    return View("Success");
                }
                else
                {
                    //Posso implementar uma view do tipo "Seu usuário não foi encontrado"
                }
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            return View(new ResetPasswordModel { Token = token, Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

                    if (!result.Succeeded)
                    {
                        foreach (var erro in result.Errors)
                        {
                            ModelState.AddModelError("", erro.Description);
                        }
                        return View();
                    }
                    return View("Success");
                }
                ModelState.AddModelError("", "Invalid Request");
            }
            return View();
        }

        [HttpGet]
        public IActionResult TwoFactor()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TwoFactor(TwoFactorModel model) //Confirmação de Login por "DoisFatores", via token.
        {
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme); //Validar se expirou ou não.

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Seu token expirou....");
                return View();
            }
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(result.Principal.FindFirstValue("sub")); //Verifica o userId

                if (user != null)
                {
                    var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, result.Principal.FindFirstValue("amr"), model.Token); //Verifica meu Email - provedor.

                    if (isValid)
                    {
                        await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

                        var claimsPrincipal = await _userClaimsPrincipalFactory.CreateAsync(user);

                        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, claimsPrincipal);

                        return RedirectToAction("About");
                    }
                    ModelState.AddModelError("", "Token Inválido!");
                    return View();
                }
                ModelState.AddModelError("", "Requisição inválida!");
            }
            return View();
        }


        [HttpGet]
        [Authorize] // Se o login não for feito e se não foi armazenado o login, não cai no authentication no startup. Ou seja, essa action irá usar uma authentication.
        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user == null)
                {
                    user = new MyUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = model.UserName,
                        Email = model.UserName
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationEmail = Url.Action("ConfirmEmailAdress", "Home", new { token = token, email = user.Email }, Request.Scheme);

                        System.IO.File.WriteAllText("confirmationEmail.txt", confirmationEmail); //É a mesma base do Forgot, onde é armazenado em um txt, o url de confirmação...
                    }
                    else
                    {
                        foreach (var erro in result.Errors)
                        {
                            ModelState.AddModelError("", erro.Description);
                        }
                        return View();
                    }
                }
                return View("Success");
            }

            return View();
        }

        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
