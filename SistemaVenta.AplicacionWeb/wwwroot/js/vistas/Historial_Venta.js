const VISTA_BUSQUEDA = {
    busquedaFecha: () => {
        $("#txtFechaInicio").val("")
        $("#txtFechaFin").val("")
        $("#txtNumeroVenta").val("")

        $(".busqueda-fecha").show()
        $(".busqueda-venta").hide()
    },
    busquedaVenta: () => {
        $("#txtFechaInicio").val("")
        $("#txtFechaFin").val("")
        $("#txtNumeroVenta").val("")

        $(".busqueda-fecha").hide()
        $(".busqueda-venta").show()
    }
}

$(document).ready(function () {
    VISTA_BUSQUEDA["busquedaFecha"]()

    $.datepicker.setDefaults($.datepicker.regional["es"])

    $("#txtFechaInicio").datepicker({ dateFormat: "dd/mm/yy" })
    $("#txtFechaFin").datepicker({ dateFormat: "dd/mm/yy" })
})

$("#cboBuscarPor").change(function () {
    if ($("#cboBuscarPor").val() == "fecha") {
        VISTA_BUSQUEDA["busquedaFecha"]()
    } else {
        VISTA_BUSQUEDA["busquedaVenta"]()
    }
})

$("#btnBuscar").click(function () {
    if ($("#cboBuscarPor").val() == "fecha") {
        if ($("#txtFechaInicio").val().trim() == "" || $("#txtFechaFin").val().trim() == "") {
            toastr.warning("", "Debe Ingresar fecha inicio y fin")
            return;
        }
    } else {
        if ($("#txtNumeroVenta").val().trim() == "") {
            toastr.warning("", "Debe Ingresar el número de venta")
            return;
        }
    }

    let numeroVenta = $("#txtNumeroVenta").val()
    let fechaInicio = $("#txtFechaInicio").val()
    let fechaFin = $("#txtFechaFin").val()

    $(".card-body").find("div.row").LoadingOverlay("show");

    fetch(`/Venta/Historial?numeroVenta=${numeroVenta}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}`)
        .then(response => {
            $(".card-body").find("div.row").LoadingOverlay("hide");

            if (!response.ok) {
                return Promise.reject(response);
            }

            return response.json();
        })
        .then(responseJson => {

            $("#tbventa tbody").html("");

            if (responseJson.length > 0) {
                responseJson.forEach((venta) => {

                    //  LÓGICA CORREGIDA: Determinar si puede facturar
                    const esTicket = parseInt(venta.idTipoDocumentoVenta) === 1;
                    const tieneUUID = venta.uuid && venta.uuid.trim() !== "";
                    const puedeFacturar = esTicket && !tieneUUID;

                    // Crear la fila base
                    let $fila = $("<tr>").append(
                        $("<td>").text(venta.fechaRegistro),
                        $("<td>").text(venta.numeroVenta),
                        $("<td>").text(venta.tipoDocumentoVenta),
                        $("<td>").text(venta.nombreCliente),
                        $("<td>").text(venta.total)
                    );

                    // Crear columna de acciones
                    let $colAcciones = $("<td>");

                    //  Botón Ver Detalles - SIEMPRE visible
                    $colAcciones.append(
                        $("<button>")
                            .addClass("btn btn-info btn-sm mr-1")
                            .append($("<i>").addClass("fas fa-eye"))
                            .data("venta", venta)
                            .attr("title", "Ver Detalles")
                    );

                    //  Botón Solicitar Factura - SOLO si puede facturar
                    if (puedeFacturar) {
                        $colAcciones.append(
                            $("<button>")
                                .addClass("btn btn-success btn-sm btn-solicitar-factura")
                                .append($("<i>").addClass("fas fa-file-invoice"))
                                .data("venta", venta)
                                .attr("title", "Solicitar Factura")
                        );
                    }

                    // Agregar columna de acciones a la fila
                    $fila.append($colAcciones);

                    // Agregar fila a la tabla
                    $("#tbventa tbody").append($fila);
                })
            }
        })
        .catch(error => {
            $(".card-body").find("div.row").LoadingOverlay("hide");

            toastr.error(
                "Verifica que API_Equipo1 esté ejecutándose.",
                "No se pudo conectar con la API."
            );

            console.error("Error al consultar historial:", error);
        });
})

//  Botón para ver detalles
$("#tbventa tbody").on("click", ".btn-info", function () {
    let d = $(this).data("venta")

    $("#txtFechaRegistro").val(d.fechaRegistro || "")
    $("#txtNumVenta").val(d.numeroVenta || "")
    $("#txtUsuarioRegistro").val(d.usuario || "")
    $("#txtTipoDocumento").val(d.tipoDocumentoVenta || "")
    $("#txtNombreCliente").val(d.nombreCliente || "")
    $("#txtSubTotal").val(d.subTotal || "0.00")
    $("#txtDescuento").val(d.descuentoTotal || "0.00")
    $("#txtIGV").val(d.impuestoTotal || "0.00")
    $("#txtTotal").val(d.total || "0.00")
    $("#txtMetodoPago").val(d.metodoPago || "")
    $("#txtFormaPago").val(d.formaPago || "")

    $("#tbProductos tbody").html("")

    d.detalleVenta.forEach((item) => {
        const cantidad = parseFloat(item.cantidad || 0)
        const precio = parseFloat(item.precio || 0)
        const subtotal = parseFloat(item.subTotal || 0)
        const descuentoDinero = parseFloat(item.descuentoDinero || 0)
        const impuestoDinero = parseFloat(item.impuestoDinero || 0)
        const total = parseFloat(item.total || 0)

        $("#tbProductos tbody").append(
            $("<tr>").append(
                $("<td>").text(item.descripcionProducto || ""),
                $("<td>").text(cantidad.toFixed(0)),
                $("<td>").text("$" + precio.toFixed(2)),
                $("<td>").text("$" + subtotal.toFixed(2)),
                $("<td>").text("$" + descuentoDinero.toFixed(2)),
                $("<td>").text("$" + impuestoDinero.toFixed(2)),
                $("<td>").text("$" + total.toFixed(2))
            )
        )
    })

    $("#linkImprimir").attr("href", `/Venta/MostrarPDFVenta?numeroVenta=${d.numeroVenta}`)
    $("#modalData").modal("show")
})

//  Botón Solicitar Factura - CORREGIDO: SOLO UNA APERTURA
$("#tbventa tbody").on("click", ".btn-solicitar-factura", function () {
    let venta = $(this).data("venta");
    let $boton = $(this);
    let $fila = $boton.closest("tr");

    swal({
        title: "¿Solicitar Factura?",
        text: `¿Desea solicitar factura para el ticket ${venta.numeroVenta}?`,
        type: "warning",
        showCancelButton: true,
        confirmButtonText: "Sí, Solicitar",
        cancelButtonText: "Cancelar",
        closeOnConfirm: false,
        closeOnCancel: true
    }, function (isConfirm) {
        if (!isConfirm) return;

        swal({
            title: "Procesando...",
            text: "Generando factura electrónica. Por favor espere.",
            type: "info",
            showConfirmButton: false,
            allowOutsideClick: false
        });


        fetch('/Venta/SolicitarFactura', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                numeroVenta: venta.numeroVenta,
                idVenta: venta.idVenta,
                soloValidar: true
            })
        })
            .then(async response => {
                const validacion = await response.json();
                if (!response.ok || !validacion.estado)
                    throw new Error(validacion.mensaje || "No se pudo validar la factura");
                return fetch('/Venta/SolicitarFactura', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ numeroVenta: venta.numeroVenta, idVenta: venta.idVenta, soloValidar: false })
                });
            })
            .then(async response => {
                const texto = await response.text();
                let data;

                try {
                    data = JSON.parse(texto);
                } catch {
                    console.error("Respuesta no JSON del servidor:", texto);
                    throw new Error(
                        `HTTP ${response.status}: El servidor devolvió una respuesta no válida. Revisa los logs de .NET.`
                    );
                }

                if (!response.ok) {
                    console.error("Estado HTTP:", response.status);
                    console.error("Error del servidor:", data);

                    throw new Error(
                        data.mensaje || `HTTP ${response.status}`
                    );
                }

                return data;
            })
            .then(data => {
                if (!data.estado) {
                    throw new Error(
                        data.mensaje || "No se pudo generar la factura"
                    );
                }

                venta.idTipoDocumentoVenta = 2;
                venta.tipoDocumentoVenta = "CFDI";
                venta.uuid = data.objeto?.uuid || "";

                swal.close();

                const pdfUrl = `/Venta/DescargarFacturaPDF?numeroVenta=${encodeURIComponent(venta.numeroVenta)}`;
                const xmlUrl = `/Venta/DescargarFacturaXML?numeroVenta=${encodeURIComponent(venta.numeroVenta)}`;

                setTimeout(function () {
                    const linkPDF = document.createElement("a");
                    linkPDF.href = pdfUrl;
                    linkPDF.download = `Factura_${venta.numeroVenta}.pdf`;
                    linkPDF.style.display = "none";
                    document.body.appendChild(linkPDF);
                    linkPDF.click();
                    document.body.removeChild(linkPDF);
                }, 300);

                setTimeout(function () {
                    const linkXML = document.createElement("a");
                    linkXML.href = xmlUrl;
                    linkXML.download = `Factura_${venta.numeroVenta}.xml`;
                    linkXML.style.display = "none";
                    document.body.appendChild(linkXML);
                    linkXML.click();
                    document.body.removeChild(linkXML);
                }, 700);

                $boton.remove();
                $fila.find("td:eq(2)").text("CFDI");

                setTimeout(function () {
                    swal({
                        title: "¡Factura Generada!",
                        text: `Factura ${venta.numeroVenta} generada exitosamente.\n\nUUID: ${venta.uuid}\n\nSe inició la descarga del PDF y XML.`,
                        type: "success",
                        confirmButtonText: "Entendido"
                    });
                }, 900);

                setTimeout(function () {
                    toastr.success(
                        "Descargas iniciadas",
                        "Factura timbrada correctamente"
                    );
                }, 1100);
            })
            .catch(error => {
                console.error("ERROR AL SOLICITAR FACTURA:", error);

                swal({
                    title: "Error al generar factura",
                    text: error.message || "Error al comunicarse con el servidor.",
                    type: "error",
                    confirmButtonText: "Cerrar"
                });
            });

    });
});
