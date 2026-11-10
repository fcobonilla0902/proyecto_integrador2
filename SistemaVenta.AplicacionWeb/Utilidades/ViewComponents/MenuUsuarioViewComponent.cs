using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SistemaVenta.AplicacionWeb.Utilidades.ViewComponents
{
    public class MenuUsuarioViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            ClaimsPrincipal claimUser = HttpContext.User;

            string nombreUsuario = "";
            string urlFotoUsuario = "/imagenes/sistema/default-user.png";

            if (claimUser.Identity != null && claimUser.Identity.IsAuthenticated)
            {
                nombreUsuario = claimUser.FindFirst(ClaimTypes.Name)?.Value ?? "";

                string? fotoClaim = claimUser.FindFirst("UrlFoto")?.Value;

                if (!string.IsNullOrWhiteSpace(fotoClaim))
                {
                    urlFotoUsuario = fotoClaim.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                                     fotoClaim.StartsWith("/")
                        ? fotoClaim
                        : "/" + fotoClaim;
                }
            }

            ViewData["nombreUsuario"] = nombreUsuario;
            ViewData["urlFotoUsuario"] = urlFotoUsuario;

            return View();
        }
    }
}