# Facturación mediante API_Equipo1

El flujo conserva el XML de entrada y el timbrado SOAP `TimbrarF` del proyecto. Consulta ventas, detalles, clientes, productos, negocio y claves SAT mediante las APIs existentes. No necesita migraciones ni columnas adicionales. La validación y las pruebas no ejecutan escrituras en SQL Server.

Se reparó `DbventaContext.cs`, que estaba truncado; el fragmento original está en `DbventaContext.cs.truncated.bak`. Los datos calculados y credenciales del negocio se excluyen del modelo EF. La facturación ya no utiliza el repositorio SQL de productos.

## Credenciales y habilitación

La aplicación ya tiene `UserSecretsId` y carga User Secrets al ejecutarse en Development. Configure localmente, sin incluir valores reales en el repositorio:

```powershell
dotnet user-secrets set "Facturacion:UsuarioPac" "<usuario>" --project SistemaVenta.AplicacionWeb
dotnet user-secrets set "Facturacion:PasswordPac" "<contraseña>" --project SistemaVenta.AplicacionWeb
dotnet user-secrets set "Facturacion:SerieFactura" "<serie>" --project SistemaVenta.AplicacionWeb
```

`ApiSettings:BaseUrl` debe apuntar a API_Equipo1. El RFC, régimen fiscal y código postal del emisor se consultan por API. No se guardan credenciales PAC en SQL ni se envían al endpoint de negocio.

El timbrado está habilitado en `appsettings.Development.json` después de completar la validación previa. En otros entornos, si no se configura el indicador, permanece deshabilitado. User Secrets puede sobrescribir este valor mediante:

```powershell
dotnet user-secrets set "Facturacion:TimbradoHabilitado" "true" --project SistemaVenta.AplicacionWeb
```

Este comando permite consumir timbres reales. No se ejecutó durante la implementación.

## Comprobación sin timbrar

Con sesión iniciada, enviar `POST /Venta/SolicitarFactura` con JSON:

```json
{"numeroVenta":"000123","idVenta":123,"soloValidar":true}
```

Valida datos fiscales, catálogos, conceptos, descuentos, impuestos y total; construye el XML sin llamar al PAC ni actualizar la venta. El botón del historial realiza esta validación antes de solicitar el timbrado. El servicio también valida antes de cualquier envío, incluida la facturación de una venta nueva.

La implementación SOAP admite objetos de impuesto 01 y 02 y factor Tasa. Configuraciones distintas se rechazan antes del envío. Una tasa cero se conserva, sin sustituirla por 16 %. La fecha de emisión es la fecha actual; la fecha original de la venta se conserva en la API.

## Archivos, estado y reintentos

El ZIP del PAC se respalda en `wwwroot/Facturas` antes de extraer PDF/XML. Se exige PDF con encabezado válido y XML con UUID. Después se actualizan UUID, fecha de timbrado y tipo de documento por API, y se vuelve a consultar la venta para confirmar el cambio. Una venta nueva permanece como ticket hasta recibir el timbrado correcto.

Los archivos `Factura_<folio>_lock`, `_pendiente`, `_pac.zip` y `_resultado.json` coordinan solicitudes y recuperación sin agregar tablas. Los respaldos de control se bloquean por HTTP; únicamente PDF/XML se sirven desde esa carpeta. No contienen credenciales PAC.

- Si falla la actualización API, solicitar nuevamente la misma venta recupera el resultado local y sincroniza su estado sin enviar otra solicitud SOAP.
- Si hubo timeout o respuesta incierta sin ZIP respaldado, se bloquea el reenvío y debe conciliarse el folio con el PAC. No elimine el marcador para forzar un timbrado.
- Un folio registrado para otra venta o emisor se rechaza. También se bloquean folios con XML histórico sin registro de control, para evitar volver a timbrarlos.
- Respalde la carpeta completa. Si hay varias instancias, deben compartir el mismo almacenamiento con bloqueo de archivos; discos independientes no coordinan los intentos.

## Pruebas

```powershell
dotnet build SolucionSistemaVenta.sln --no-restore
dotnet run --project tests/Facturacion.Verificacion/Facturacion.Verificacion.csproj
dotnet run --project tests/Facturacion.Verificacion/Facturacion.Verificacion.csproj -- --validar-api-real
```

El ejecutable de verificación utiliza API y PAC simulados. Las 18 comprobaciones pasaron: modelo EF sin conexión, validación, IVA cero, timbrado deshabilitado, archivos/UUID, reintentos, colisión de folios, concurrencia, respuesta incierta, XML sin UUID, totales incompatibles, correspondencia entre ID y folio, recuperación tras fallo del PUT y alta de factura nueva exclusivamente por API. No usa las credenciales reales ni consume timbres.

También se comprobó el arranque de la aplicación: `GET /Acceso/Login` respondió HTTP 200 y el acceso HTTP a los archivos de control de facturación respondió 404. La prueba de arranque se detuvo al terminar.

La comprobación contra `http://localhost:5096/` también pasó: la venta 64 superó `SoloValidar` utilizando los datos reales de ventas, clientes, productos, negocio y catálogos. Este modo bloquea POST, PUT y DELETE, y comprueba que no haya llamadas al PAC.

API_Equipo1 se compiló desde una copia de sus fuentes en `.verification/API_Equipo1`, sin modificar el proyecto externo. Sus contratos actuales ya contienen los campos necesarios para confirmar la factura. Queda pendiente cualquier timbrado contra el PAC real con las credenciales del entorno; las pruebas y la validación previa no certifican aceptación fiscal del comprobante.
