using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity;
using SistemaVenta.Entity.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaVenta.BLL.Implementacion
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly IGenericRepository<Configuracion> _repositorio;

        public CloudinaryService(IGenericRepository<Configuracion> repositorio)
        {
            _repositorio = repositorio;
        }

        public async Task<string> SubirStorage(Stream StreamArchivo, string CarpetaDestino, string NombreArchivo)
        {
            string UrlImagen = "";
            try
            {
                IQueryable<Configuracion> query = await _repositorio.Consultar(c => c.Recurso.Equals("Cloudinary"));
                Dictionary<string, string> Config = query.ToDictionary(keySelector: c => c.Propiedad, elementSelector: c => c.Valor);

                var account = new Account(
                    Config["cloud_name"],
                    Config["api_key"],
                    Config["api_secret"]
                );

                var cloudinary = new Cloudinary(account);

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(NombreArchivo, StreamArchivo),
                    Folder = CarpetaDestino, // Se usa la variable CarpetaDestino directamente
                    PublicId = NombreArchivo
                };

                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.Url?.ToString()))
                {
                    UrlImagen = uploadResult.Url.ToString();
                }
            }
            catch
            {
                UrlImagen = "";
            }

            return UrlImagen;
        }

        public async Task<bool> EliminarStorage(string CarpetaDestino, string NombreArchivo)
        {
            try
            {
                IQueryable<Configuracion> query = await _repositorio.Consultar(c => c.Recurso.Equals("Cloudinary"));
                Dictionary<string, string> Config = query.ToDictionary(keySelector: c => c.Propiedad, elementSelector: c => c.Valor);

                var account = new Account(
                    Config["cloud_name"],
                    Config["api_key"],
                    Config["api_secret"]
                );

                var cloudinary = new Cloudinary(account);

                // La corrección clave: se crea el publicId uniendo la CarpetaDestino y el NombreArchivo directamente.
                var publicId = $"{CarpetaDestino}/{NombreArchivo}";
                var deletionParams = new DeletionParams(publicId);

                var deletionResult = await cloudinary.DestroyAsync(deletionParams);

                return deletionResult.Result == "ok";
            }
            catch
            {
                return false;
            }
        }
    }
}