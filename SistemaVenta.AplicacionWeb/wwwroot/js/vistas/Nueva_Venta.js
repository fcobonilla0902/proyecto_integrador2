let ValorImpuesto = 0;
let ProductosParaVenta = [];
let impuestoListo = false;

$(document).ready(function () {

    // txtDescuento es solo de lectura; no necesitamos escuchar input del usuario
    // mostrarProducto_Precios(); // Se llama cuando agregas/eliminás productos

    // ===============================
    // OBTENER DATOS DE NEGOCIO (Impuesto)
    // ===============================
    fetch("/Negocio/Obtener")
        .then(res => res.ok ? res.json() : Promise.reject(res))
        .then(resJson => {
            if (resJson.estado) {
                const d = resJson.objeto;

                ValorImpuesto = parseFloat(d.porcentajeImpuesto);
                impuestoListo = true;

                console.log("Impuesto cargado correctamente:", ValorImpuesto);
                activarSelectorProductos();
            } else {
                console.error("No se pudo cargar el impuesto, usando 16%");
                impuestoListo = true;
                ValorImpuesto = 16;
                activarSelectorProductos();
            }
        })
        .catch(() => {
            console.error("Error al cargar impuesto, usando 16%");
            impuestoListo = true;
            ValorImpuesto = 16;
            activarSelectorProductos();
        });

    // ======================================
    // SELECT para buscar cliente
    // ======================================
    $("#cboBuscarCliente").select2({
        ajax: {
            url: "/Cliente/BuscarClientes",
            dataType: "json",
            delay: 250,
            data: params => ({
                busqueda: params.term
            }),
            processResults: function (data) {
                // IMPORTANTE: aquí asumimos que tu endpoint MVC ya hace
                // el mapeo a estas propiedades (nombreCompleto, correo, etc.)
                return {
                    results: data.map(item => ({
                        id: item.idCliente,
                        text: item.nombreCompleto,
                        correo: item.correo,
                        rfc: item.rfc,
                        domicilioFiscal: item.domicilioFiscalReceptor,
                        regimenFiscal: item.regimenFiscalReceptor,
                        fechaRegistro: item.fechaRegistro,
                        pais: item.pais,
                        estado: item.estado,
                        municipio: item.municipio
                    }))
                };
            }
        },
        placeholder: "Buscar Cliente...",
        minimumInputLength: 1,
        language: "es"
    });

    $("#cboBuscarCliente").on("select2:select", function (e) {
        const data = e.params.data;

        $("#txtNombreCliente").val(data.text);
        $("#txtCorreoCliente").val(data.correo);
        $("#txtRfcCliente").val(data.rfc);
        $("#txtDomicilioFiscalCliente").val(data.domicilioFiscal);
        $("#txtRegimenFiscalCliente").val(data.regimenFiscal);
        $("#txtFechaRegistroCliente").val(data.fechaRegistro);
        $("#txtPaisCliente").val(data.pais ?? "");
        $("#txtEstadoCliente").val(data.estado ?? "");
        $("#txtMunicipioCliente").val(data.municipio ?? "");
    });

    cargarCombosFiscales();
});

function cargarCombosFiscales() {

    fetch("/Venta/ListaMetodoPago")
        .then(res => res.ok ? res.json() : Promise.reject(res))
        .then(data => {
            $("#cboMetodoPago").html('<option value="">Seleccione...</option>');

            data.forEach(item => {
                $("#cboMetodoPago").append(
                    $("<option>").val(item.idMetodoPago).text(item.descripcion)
                );
            });
        })
        .catch(error => {
            console.error("Error al cargar métodos de pago:", error);
            toastr.error("", "No se pudieron cargar los métodos de pago");
        });

    fetch("/Venta/ListaUsoCFDI")
        .then(res => res.ok ? res.json() : Promise.reject(res))
        .then(data => {
            $("#cboUsoCFDI").html('<option value="">Seleccione...</option>');

            data.forEach(item => {
                $("#cboUsoCFDI").append(
                    $("<option>").val(item.idUsoCFDI).text(item.descripcion)
                );
            });
        })
        .catch(error => {
            console.error("Error al cargar usos CFDI:", error);
            toastr.error("", "No se pudieron cargar los usos CFDI");
        });

    fetch("/Venta/ListaFormaPago")
        .then(res => res.ok ? res.json() : Promise.reject(res))
        .then(data => {
            $("#cboFormaPago").html('<option value="">Seleccione...</option>');

            data.forEach(item => {
                $("#cboFormaPago").append(
                    $("<option>").val(item.idFormaPago).text(item.descripcion)
                );
            });
        })
        .catch(error => {
            console.error("Error al cargar formas de pago:", error);
            toastr.error("", "No se pudieron cargar las formas de pago");
        });
}

// =====================================================
// ACTIVAR SELECTOR DE PRODUCTOS
// =====================================================
function activarSelectorProductos() {

    $("#cboBuscarProducto").select2({
        ajax: {
            url: "/Venta/ObtenerProductos",
            dataType: 'json',
            contentType: "application/json; charset=utf-8",
            delay: 250,
            data: params => ({ busqueda: params.term }),
            processResults: function (data) {
                return {
                    results: data.map(item => ({
                        id: item.idProducto,
                        text: item.descripcion,
                        marca: item.marca,
                        categoria: item.nombreCategoria,
                        urlImagen: item.urlImagen,
                        precio: parseFloat(item.precio),
                        descuento: parseFloat(item.descuento) || 0
                    }))
                };
            }
        },
        language: "es",
        placeholder: 'Buscar Producto...',
        minimumInputLength: 1,
        templateResult: formatoResultados
    });

    // =====================================================
    // SELECCIONAR PRODUCTO
    // =====================================================
    $("#cboBuscarProducto").on("select2:select", function (e) {

        if (!impuestoListo) {
            toastr.warning("", "Cargando impuesto, intenta en un segundo...");
            return;
        }

        const data = e.params.data;

        if (ProductosParaVenta.some(p => p.idProducto == data.id)) {
            $("#cboBuscarProducto").val("").trigger("change");
            toastr.warning("", "El producto ya fue agregado");
            return;
        }

        swal({
            title: data.marca,
            text: data.text,
            imageUrl: data.urlImagen,
            type: "input",
            showCancelButton: true,
            closeOnConfirm: false,
            inputPlaceholder: "Ingrese Cantidad"
        },
            function (valor) {

                if (valor === false) return false;

                if (valor === "" || isNaN(parseInt(valor))) {
                    toastr.warning("", "Ingrese una cantidad válida");
                    return false;
                }

                // CALCULAR PRECIO CON DESCUENTO AUTOMÁTICO
                let cantidad = parseInt(valor);
                let precioUnitario = parseFloat(data.precio) || 0;
                let descuentoPorcentaje = parseFloat(data.descuento) || 0; // 35 => 35%
                let impuestoPorcentaje = parseFloat(ValorImpuesto) || 16;   // 16 => 16%

                let subtotalLinea = cantidad * precioUnitario;
                let descuentoDecimal = descuentoPorcentaje / 100;
                let descuentoDinero = subtotalLinea * descuentoDecimal;

                let baseImpuesto = subtotalLinea - descuentoDinero;
                let impuestoDecimal = impuestoPorcentaje / 100;
                let impuestoDinero = baseImpuesto * impuestoDecimal;

                let totalLinea = baseImpuesto + impuestoDinero;

                let producto = {
                    idProducto: data.id,
                    marcaProducto: data.marca,
                    descripcionProducto: data.text,
                    categoriaProducto: data.categoria,

                    cantidad: cantidad,
                    precio: precioUnitario,

                    descuentoProducto: descuentoPorcentaje,      // 35
                    descuentoDecimal: descuentoDecimal,          // 0.35
                    descuentoDinero: descuentoDinero,            // 2730.00

                    impuestoProducto: impuestoPorcentaje,        // 16
                    impuestoDecimal: impuestoDecimal,            // 0.16
                    impuestoDinero: impuestoDinero,              // 811.20

                    subtotal: subtotalLinea,                     // antes de descuento/impuesto
                    baseImpuesto: baseImpuesto,                  // subtotal - descuento
                    total: totalLinea                            // total final de la línea
                };

                ProductosParaVenta.push(producto);
                mostrarProducto_Precios();
                $("#cboBuscarProducto").val("").trigger("change");
                swal.close();
            }
        );
    });
}

// =====================================================
// FORMATO VISUAL PARA LOS PRODUCTOS
// =====================================================
function formatoResultados(data) {

    if (data.loading)
        return data.text;

    return $(`
        <table width="100%">
            <tr>
                <td style="width:60px">
                    <img style="height:60px;width:60px;margin-right:10px" src="${data.urlImagen}" />
                </td>
                <td>
                    <p style="font-weight:bold;margin:2px">${data.marca}</p>
                    <p style="margin:2px">${data.text}</p>
                </td>
            </tr>
        </table>
    `);
}

// =====================================================
// MOSTRAR PRODUCTOS + CALCULAR SUBTOTAL, IVA Y TOTAL
// =====================================================
function mostrarProducto_Precios() {

    if (!ValorImpuesto || isNaN(ValorImpuesto)) {
        ValorImpuesto = 16;
    }

    let subtotalBrutoGeneral = 0;
    let descuentoTotalDinero = 0;
    let baseGravableGeneral = 0;
    let impuestoTotalDinero = 0;
    let totalGeneral = 0;

    $("#tbProducto tbody").html("");

    ProductosParaVenta.forEach((item) => {
        subtotalBrutoGeneral += item.subtotal;
        descuentoTotalDinero += item.descuentoDinero;
        baseGravableGeneral += item.baseImpuesto;
        impuestoTotalDinero += item.impuestoDinero;
        totalGeneral += item.total;

        $("#tbProducto tbody").append(
            $("<tr>").append(
                $("<td>").append(
                    $("<button>")
                        .addClass("btn btn-danger btn-eliminar btn-sm")
                        .append($("<i>").addClass("fas fa-trash-alt"))
                        .data("idProducto", item.idProducto)
                ),
                $("<td>").text(item.descripcionProducto),
                $("<td>").text(item.cantidad),
                $("<td>").text("$" + item.precio.toFixed(2)),
                $("<td>").text("$" + item.subtotal.toFixed(2)),
                $("<td>").html(
                    "$" + item.descuentoDinero.toFixed(2) +
                    `<br><small class="text-success">${item.descuentoProducto.toFixed(2)}%</small>`
                ),
                $("<td>").html(
                    "$" + item.impuestoDinero.toFixed(2) +
                    `<br><small class="text-muted">${item.impuestoProducto.toFixed(2)}%</small>`
                ),
                $("<td>").text("$" + item.total.toFixed(2))
            )
        );
    });

    $("#txtSubTotal").val("$" + subtotalBrutoGeneral.toFixed(2));
    $("#txtDescuento").val("$" + descuentoTotalDinero.toFixed(2));
    $("#txtIGV").val("$" + impuestoTotalDinero.toFixed(2));
    $("#txtTotal").val("$" + totalGeneral.toFixed(2));
}

// =====================================================
// ELIMINAR PRODUCTO
// =====================================================
$(document).on("click", "button.btn-eliminar", function () {
    const id = $(this).data("idProducto");
    ProductosParaVenta = ProductosParaVenta.filter(x => x.idProducto != id);
    mostrarProducto_Precios();
});

// =====================================================
// GUARDAR VENTA - CON VALIDACIÓN DE FACTURA
// =====================================================
$("#btnTerminarVenta").click(function () {

    if (ProductosParaVenta.length < 1) {
        toastr.warning("", "Debe ingresar productos");
        return;
    }

    const tipoDocumento = $("#cboTipoDocumento").val();

    const idMetodoPago = $("#cboMetodoPago").val();
    const idUsoCFDI = $("#cboUsoCFDI").val();
    const idFormaPago = $("#cboFormaPago").val();

    if (!idMetodoPago || !idUsoCFDI || !idFormaPago) {
        swal({
            title: "Datos fiscales incompletos",
            text: "Debe seleccionar Método de pago, Uso CFDI y Forma de pago.",
            type: "warning",
            confirmButtonText: "Entendido"
        });
        return;
    }

    //  SI ES FACTURA, VALIDAR DATOS FISCALES
    if (tipoDocumento == "2") {
        const rfcCliente = $("#txtRfcCliente").val().trim();
        const domicilioFiscal = $("#txtDomicilioFiscalCliente").val().trim();
        const regimenFiscal = $("#txtRegimenFiscalCliente").val().trim();

        if (!rfcCliente || !domicilioFiscal || !regimenFiscal) {
            swal({
                title: "Datos Fiscales Incompletos",
                text: "Para emitir una factura debe seleccionar un cliente con:\n• RFC\n• Domicilio Fiscal\n• Régimen Fiscal",
                type: "warning",
                confirmButtonText: "Entendido"
            });
            return;
        }
    }

    // Mapear ProductosParaVenta al formato esperado por el backend
    const detalleVenta = ProductosParaVenta.map(item => ({
        idProducto: item.idProducto,
        descripcionProducto: item.descripcionProducto,
        cantidad: item.cantidad,
        precio: Number(item.precio.toFixed(2)),
        subTotal: Number(item.subtotal.toFixed(2)),

        descuento: Number(item.descuentoDecimal.toFixed(6)),
        descuentoDinero: Number(item.descuentoDinero.toFixed(2)),

        impuesto: Number(item.impuestoDecimal.toFixed(6)),
        impuestoDinero: Number(item.impuestoDinero.toFixed(2)),

        total: Number(item.total.toFixed(2))
    }));

    console.log("detalleVenta enviado:", detalleVenta);

    const venta = {
        idTipoDocumentoVenta: parseInt(tipoDocumento),
        nombreCliente: $("#txtNombreCliente").val(),
        idCliente: $("#cboBuscarCliente").val() || null,

        idMetodoPago: parseInt(idMetodoPago),
        idUsoCFDI: parseInt(idUsoCFDI),
        idFormaPago: parseInt(idFormaPago),

        subTotal: $("#txtSubTotal").val().replace("$", "").trim(),
        descuentoTotal: $("#txtDescuento").val(),
        impuestoTotal: $("#txtIGV").val().replace("$", "").trim(),
        total: $("#txtTotal").val().replace("$", "").trim(),
        detalleVenta: detalleVenta
    };

    $("#btnTerminarVenta").LoadingOverlay("show");

    fetch("/Venta/RegistrarVenta", {
        method: "POST",
        headers: { "Content-Type": "application/json; charset=utf-8" },
        body: JSON.stringify(venta)
    })
        .then(res => res.ok ? res.json() : Promise.reject(res))
        .then(resJson => {

            $("#btnTerminarVenta").LoadingOverlay("hide");

            if (resJson.estado) {

                ProductosParaVenta = [];
                mostrarProducto_Precios();

                $("#txtNombreCliente").val("");
                $("#txtCorreoCliente").val("");
                $("#txtRfcCliente").val("");
                $("#txtDomicilioFiscalCliente").val("");
                $("#txtRegimenFiscalCliente").val("");
                $("#txtFechaRegistroCliente").val("");
                $("#txtPaisCliente").val("");
                $("#txtEstadoCliente").val("");
                $("#txtMunicipioCliente").val("");
                $("#txtDescuento").val("$0.00");
                $("#txtSubTotal").val("$0.00");
                $("#txtIGV").val("$0.00");
                $("#txtTotal").val("$0.00");
                $("#cboTipoDocumento").val("1");
                $("#cboMetodoPago").val("");
                $("#cboUsoCFDI").val("");
                $("#cboFormaPago").val("");
                $("#cboBuscarCliente").val("").trigger("change");

                if (tipoDocumento == "2") {
                    const numeroVenta = resJson.objeto.numeroVenta || "N/D";
                    const uuid = resJson.objeto.uuid || "No disponible";

                    swal({
                        title: "¡Factura Timbrada!",
                        text: `Número de Venta: ${numeroVenta}<br>UUID: ${uuid}<br><br>La factura se ha generado correctamente.`,
                        type: "success",
                        html: true,
                        confirmButtonText: "Ver Factura",
                        showCancelButton: true,
                        cancelButtonText: "Cerrar"
                    }, function (isConfirm) {
                        if (isConfirm) {
                            window.open(`/Venta/DescargarFacturaPDF?numeroVenta=${numeroVenta}`, '_blank');
                        }
                    });
                } else {
                    // ES TICKET
                    swal({
                        title: "¡Registrado!",
                        text: `Número de Venta: ${resJson.objeto.numeroVenta}`,
                        type: "success",
                        showCancelButton: false,
                        confirmButtonText: "Continuar"
                    });
                }

            } else {
                swal("Error", resJson.mensaje || "No se pudo registrar la venta", "error");
            }

        })
        .catch(error => {
            $("#btnTerminarVenta").LoadingOverlay("hide");
            console.error("Error:", error);
            swal("Error", "Ocurrió un error al registrar la venta", "error");
        });
});