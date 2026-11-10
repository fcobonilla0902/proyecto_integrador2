using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;

namespace SistemaVenta.AplicacionWeb.Utilidades.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly IApiClient _api;

        public MenuViewComponent(IApiClient api)
        {
            _api = api;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            List<VMMenu> listaMenus = new();

            try
            {
                var claimUser = HttpContext.User;

                if (claimUser.Identity == null || !claimUser.Identity.IsAuthenticated)
                    return View(listaMenus);

                string? idUsuarioApi = claimUser.Claims
                    .Where(c => c.Type == "IdUsuarioApi")
                    .Select(c => c.Value)
                    .SingleOrDefault();

                string? idUsuario = !string.IsNullOrWhiteSpace(idUsuarioApi)
                    ? idUsuarioApi
                    : claimUser.Claims
                        .Where(c => c.Type == ClaimTypes.NameIdentifier)
                        .Select(c => c.Value)
                        .SingleOrDefault();

                if (string.IsNullOrWhiteSpace(idUsuario))
                    return View(listaMenus);

                if (!int.TryParse(idUsuario, out int idUsuarioInt))
                    return View(listaMenus);

                var usuariosApi = await _api.GetAsync<List<UsuarioApiDto>>("api/usuario?skip=0&take=99999")
                                 ?? new List<UsuarioApiDto>();

                var usuarioActual = usuariosApi.FirstOrDefault(u => u.IdUsuario == idUsuarioInt);

                if (usuarioActual == null || !usuarioActual.IdRol.HasValue)
                    return View(listaMenus);

                int idRol = usuarioActual.IdRol.Value;

                var rolMenusApi = await _api.GetAsync<List<RolMenuApiDto>>("api/rolmenu?skip=0&take=99999")
                                  ?? new List<RolMenuApiDto>();

                var menusApi = await _api.GetAsync<List<MenuApiDto>>("api/menu?skip=0&take=99999")
                               ?? new List<MenuApiDto>();

                menusApi = menusApi
                    .Where(m => m != null && m.EsActivo != 0)
                    .GroupBy(m => m.IdMenu)
                    .Select(g => g.First())
                    .ToList();

                List<MenuApiDto> menusRol;

                if (idRol == 1)
                {
                    menusRol = menusApi
                        .GroupBy(x => x.IdMenu)
                        .Select(g => g.First())
                        .ToList();
                }
                else
                {
                    var idsMenusPermitidos = rolMenusApi
                        .Where(rm => rm.IdRol == idRol && rm.IdMenu.HasValue && rm.EsActivo != 0)
                        .Select(rm => rm.IdMenu!.Value)
                        .Distinct()
                        .ToHashSet();

                    menusRol = menusApi
                        .Where(m => idsMenusPermitidos.Contains(m.IdMenu))
                        .GroupBy(x => x.IdMenu)
                        .Select(g => g.First())
                        .ToList();
                }

                if (!menusRol.Any())
                    return View(listaMenus);

                var menusPadre = menusRol
                    .Where(m => m.IdMenuPadre == null || m.IdMenuPadre == 0 || m.IdMenu == m.IdMenuPadre)
                    .ToList();

                var menusHijo = menusRol
                    .Where(m => m.IdMenuPadre != null && m.IdMenuPadre != 0 && m.IdMenu != m.IdMenuPadre)
                    .ToList();

                if (!menusPadre.Any() && menusHijo.Any())
                {
                    var idsPadre = menusHijo
                        .Where(h => h.IdMenuPadre.HasValue)
                        .Select(h => h.IdMenuPadre!.Value)
                        .Distinct()
                        .ToList();

                    menusPadre = menusApi
                        .Where(m => idsPadre.Contains(m.IdMenu))
                        .GroupBy(x => x.IdMenu)
                        .Select(g => g.First())
                        .ToList();
                }

                var menuAdministracion = menusPadre.FirstOrDefault(m => EsAdministracion(m.Descripcion));

                if (menuAdministracion != null)
                {
                    var hijosReubicables = menusRol
                        .Where(m =>
                            m.IdMenu != menuAdministracion.IdMenu &&
                            !menusHijo.Any(h => h.IdMenu == m.IdMenu) &&
                            DebeIrEnAdministracion(m))
                        .ToList();

                    foreach (var item in hijosReubicables)
                    {
                        if (!menusHijo.Any(h => h.IdMenu == item.IdMenu))
                        {
                            item.IdMenuPadre = menuAdministracion.IdMenu;
                            menusHijo.Add(item);
                        }

                        menusPadre.RemoveAll(p => p.IdMenu == item.IdMenu);
                    }
                }

                var padresIds = menusPadre.Select(p => p.IdMenu).ToHashSet();

                var huerfanos = menusHijo
                    .Where(h => !h.IdMenuPadre.HasValue || !padresIds.Contains(h.IdMenuPadre.Value))
                    .ToList();

                foreach (var huerfano in huerfanos)
                {
                    if (menuAdministracion != null && DebeIrEnAdministracion(huerfano))
                    {
                        huerfano.IdMenuPadre = menuAdministracion.IdMenu;
                    }
                    else
                    {
                        if (!menusPadre.Any(p => p.IdMenu == huerfano.IdMenu))
                            menusPadre.Add(huerfano);
                    }
                }

                menusPadre = menusPadre
                    .GroupBy(x => x.IdMenu)
                    .Select(g => g.First())
                    .ToList();

                menusHijo = menusHijo
                    .GroupBy(x => x.IdMenu)
                    .Select(g => g.First())
                    .ToList();

                foreach (var padre in menusPadre
                    .OrderBy(x => ObtenerOrdenMenu(x.Descripcion))
                    .ThenBy(x => x.Descripcion))
                {
                    var hijos = menusHijo
                        .Where(h => h.IdMenuPadre == padre.IdMenu)
                        .GroupBy(h => h.IdMenu)
                        .Select(g => g.First())
                        .OrderBy(h => ObtenerOrdenSubMenu(padre.Descripcion, h.Descripcion))
                        .ThenBy(h => h.Descripcion)
                        .ToList();

                    listaMenus.Add(new VMMenu
                    {
                        Descripcion = padre.Descripcion ?? "",
                        Icono = padre.Icono ?? "",
                        Controlador = padre.Controlador ?? "",
                        PaginaAccion = hEsValida(padre.PaginaAccion) ? padre.PaginaAccion! : "",
                        SubMenus = hijos.Select(h => new VMMenu
                        {
                            Descripcion = h.Descripcion ?? "",
                            Icono = h.Icono ?? "",
                            Controlador = h.Controlador ?? "",
                            PaginaAccion = hEsValida(h.PaginaAccion) ? h.PaginaAccion! : "",
                            SubMenus = new List<VMMenu>()
                        }).ToList()
                    });
                }

                if (!listaMenus.Any() && menusRol.Any())
                {
                    listaMenus = menusRol
                        .GroupBy(x => x.IdMenu)
                        .Select(g => g.First())
                        .OrderBy(x => ObtenerOrdenMenu(x.Descripcion))
                        .ThenBy(x => x.Descripcion)
                        .Select(m => new VMMenu
                        {
                            Descripcion = m.Descripcion ?? "",
                            Icono = m.Icono ?? "",
                            Controlador = m.Controlador ?? "",
                            PaginaAccion = hEsValida(m.PaginaAccion) ? m.PaginaAccion! : "",
                            SubMenus = new List<VMMenu>()
                        })
                        .ToList();
                }

                ViewBag.ApiNoDisponible = false;
                return View(listaMenus);
            }
            catch (Exception)
            {
                ViewBag.ApiNoDisponible = true;
                return View(listaMenus);
            }
        }

        private static bool DebeIrEnAdministracion(MenuApiDto menu)
        {
            var descripcion = Normalizar(menu.Descripcion);
            var controlador = Normalizar(menu.Controlador);
            var paginaAccion = Normalizar(menu.PaginaAccion);

            if (descripcion == "clientes" || descripcion == "cliente")
                return true;

            if (controlador == "cliente" || controlador == "clientes")
                return true;

            if (paginaAccion == "cliente" || paginaAccion == "clientes")
                return true;

            return false;
        }

        private static bool EsAdministracion(string? descripcion)
        {
            var texto = Normalizar(descripcion);
            return texto == "administracion";
        }

        private static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            texto = texto.Trim().ToLowerInvariant();

            texto = texto
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u");

            return texto;
        }

        private static bool hEsValida(string? texto)
        {
            return !string.IsNullOrWhiteSpace(texto);
        }

        private static int ObtenerOrdenMenu(string? descripcion)
        {
            var texto = Normalizar(descripcion);

            if (texto == "dashboard")
                return 1;

            if (texto == "administracion")
                return 2;

            if (texto == "inventario")
                return 3;

            if (texto == "ventas")
                return 4;

            if (texto == "reportes")
                return 5;

            return 999;
        }

        private static int ObtenerOrdenSubMenu(string? menuPadre, string? descripcion)
        {
            var padre = Normalizar(menuPadre);
            var texto = Normalizar(descripcion);

            if (padre == "administracion")
            {
                if (texto == "negocio")
                    return 1;

                if (texto == "usuarios")
                    return 2;

                if (texto == "clientes" || texto == "cliente")
                    return 3;
            }

            if (padre == "ventas")
            {
                if (texto == "nueva venta")
                    return 1;

                if (texto == "historial venta")
                    return 2;
            }

            return 999;
        }

        private class UsuarioApiDto
        {
            public int IdUsuario { get; set; }
            public string? Nombre { get; set; }
            public string? Correo { get; set; }
            public string? Telefono { get; set; }
            public int? IdRol { get; set; }
            public string? UrlFoto { get; set; }
            public string? NombreFoto { get; set; }
            public string? Clave { get; set; }
            public DateTime? FechaRegistro { get; set; }
            public int? IdStatusRegistro { get; set; }
        }

        private class MenuApiDto
        {
            public int IdMenu { get; set; }
            public string? Descripcion { get; set; }
            public string? Icono { get; set; }
            public string? Controlador { get; set; }
            public string? PaginaAccion { get; set; }
            public int? IdMenuPadre { get; set; }
            public int? EsActivo { get; set; }
        }

        private class RolMenuApiDto
        {
            public int IdRolMenu { get; set; }
            public int? IdRol { get; set; }
            public int? IdMenu { get; set; }
            public int? EsActivo { get; set; }
            public DateTime? FechaRegistro { get; set; }
        }
    }
}