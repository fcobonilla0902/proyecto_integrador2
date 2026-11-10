using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SistemaVenta.BLL.Interfaces;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity;
using SistemaVenta.Entity.Models;

namespace SistemaVenta.BLL.Implementacion
{
    public class MenuService : IMenuService
    {
        private readonly IGenericRepository<Menu> _repositorioMenu;
        private readonly IGenericRepository<RolMenu> _repositorioRolMenu;
        private readonly IGenericRepository<Usuario> _repositorioUsuario;

        public MenuService(
            IGenericRepository<Menu> repositorioMenu,
            IGenericRepository<RolMenu> repositorioRolMenu,
            IGenericRepository<Usuario> repositorioUsuario
            )
        {
            _repositorioMenu = repositorioMenu;
            _repositorioRolMenu = repositorioRolMenu;
            _repositorioUsuario = repositorioUsuario;
        }

        public async Task<List<Menu>> ObtenerMenus(int idUsuario)
        {
            IQueryable<Usuario> tbUsuario = await _repositorioUsuario.Consultar(u => u.IdUsuario == idUsuario);
            IQueryable<RolMenu> tbRolMenu = await _repositorioRolMenu.Consultar();
            IQueryable<Menu> tbMenu = await _repositorioMenu.Consultar();

            int? idRol = tbUsuario.Select(u => u.IdRol).FirstOrDefault();

            Console.WriteLine("==== DEBUG MENU SERVICE ====");
            Console.WriteLine($"idUsuario: {idUsuario}");
            Console.WriteLine($"idRol: {(idRol.HasValue ? idRol.Value.ToString() : "NULL")}");

            if (idRol == null)
            {
                Console.WriteLine("SALIO PORQUE idRol ES NULL");
                return new List<Menu>();
            }

            List<Menu> menusRol = (from rm in tbRolMenu
                                   join m in tbMenu on rm.IdMenu equals m.IdMenu
                                   where rm.IdRol == idRol
                                   select m).Distinct().ToList();

            Console.WriteLine($"menusRol: {menusRol.Count}");

            foreach (var item in menusRol)
            {
                Console.WriteLine($"MenuRol -> Id:{item.IdMenu}, Desc:{item.Descripcion}, Padre:{item.IdMenuPadre}");
            }

            List<Menu> menusPadre = menusRol
                .Where(m => m.IdMenuPadre == null || m.IdMenuPadre == 0 || m.IdMenu == m.IdMenuPadre)
                .ToList();

            List<Menu> menusHijo = menusRol
                .Where(m => m.IdMenuPadre != null && m.IdMenuPadre != 0 && m.IdMenu != m.IdMenuPadre)
                .ToList();

            Console.WriteLine($"menusPadre: {menusPadre.Count}");
            Console.WriteLine($"menusHijo: {menusHijo.Count}");

            foreach (var padre in menusPadre)
            {
                padre.InverseIdMenuPadreNavigation = menusHijo
                    .Where(h => h.IdMenuPadre == padre.IdMenu)
                    .ToList();
            }

            return menusPadre;
        }
    }
}
