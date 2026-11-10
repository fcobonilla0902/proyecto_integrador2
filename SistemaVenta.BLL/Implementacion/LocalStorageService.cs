using SistemaVenta.BLL.Interfaces;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity;
using SistemaVenta.Entity.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVenta.BLL.Implementacion
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly string _basePath;
        private readonly string _baseUrl;

        public LocalStorageService(string webRootPath)
        {
            _basePath = Path.Combine(webRootPath, "imagenes");
            _baseUrl = "/imagenes";
        }

        public async Task<string> SubirStorage(Stream archivoStream, string carpetaDestino, string nombreArchivo)
        {
            try
            {
                // 🔄 Asegura un nombre único para evitar caché
                string extension = Path.GetExtension(nombreArchivo);
                string nombreUnico = $"{Path.GetFileNameWithoutExtension(nombreArchivo)}_{DateTime.Now.Ticks}{extension}";

                string carpetaPath = Path.Combine(_basePath, carpetaDestino);

                if (!Directory.Exists(carpetaPath))
                    Directory.CreateDirectory(carpetaPath);

                string rutaArchivo = Path.Combine(carpetaPath, nombreUnico);

                using (var fileStream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await archivoStream.CopyToAsync(fileStream);
                }

                // Devuelve la URL con el nuevo nombre
                string url = $"{_baseUrl}/{carpetaDestino}/{nombreUnico}".Replace("\\", "/");
                return url;
            }
            catch
            {
                return "";
            }
        }


        public async Task<bool> EliminarStorage(string carpetaDestino, string nombreArchivo)
        {
            try
            {
                string rutaArchivo = Path.Combine(_basePath, carpetaDestino, nombreArchivo);

                if (File.Exists(rutaArchivo))
                {
                    File.Delete(rutaArchivo);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
