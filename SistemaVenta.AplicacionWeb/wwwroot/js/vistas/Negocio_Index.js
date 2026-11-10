let catalogoPaises = [];
let catalogoEstados = [];
let catalogoMunicipios = [];

$(document).ready(function () {
    $(".card-body").LoadingOverlay("show");

    Promise.all([
        cargarRegimenesFiscales(),
        cargarPaises(),
        cargarEstados(),
        cargarMunicipios()
    ])
        .then(() => obtenerNegocio())
        .finally(() => $(".card-body").LoadingOverlay("hide"));
});

function cargarRegimenesFiscales() {
    return fetch("/Negocio/ListaRegimenFiscal")
        .then(response => response.ok ? response.json() : Promise.reject(response))
        .then(responseJson => {
            $("#cbRegimenFiscal").empty();
            $("#cbRegimenFiscal").append($("<option>").val("").text("Seleccione..."));

            responseJson.forEach(item => {
                $("#cbRegimenFiscal").append(
                    $("<option>")
                        .val(item.codigo)
                        .text(item.texto)
                );
            });
        })
        .catch(err => {
            console.error("Error cargando régimen fiscal:", err);
            swal("Error", "No se pudo cargar el catálogo de régimen fiscal.", "error");
        });
}

function cargarPaises() {
    return fetch("/Negocio/ListaPaises")
        .then(response => response.ok ? response.json() : Promise.reject(response))
        .then(responseJson => {
            catalogoPaises = responseJson || [];
            llenarPaises();
        })
        .catch(err => {
            console.error("Error cargando países:", err);
            swal("Error", "No se pudo cargar el catálogo de países.", "error");
        });
}

function cargarEstados() {
    return fetch("/Negocio/ListaEstados")
        .then(response => response.ok ? response.json() : Promise.reject(response))
        .then(responseJson => {
            catalogoEstados = responseJson || [];
            llenarEstados();
        })
        .catch(err => {
            console.error("Error cargando estados:", err);
            swal("Error", "No se pudo cargar el catálogo de estados.", "error");
        });
}

function cargarMunicipios() {
    return fetch("/Negocio/ListaMunicipios")
        .then(response => response.ok ? response.json() : Promise.reject(response))
        .then(responseJson => {
            catalogoMunicipios = responseJson || [];
            llenarMunicipios();
        })
        .catch(err => {
            console.error("Error cargando municipios:", err);
            swal("Error", "No se pudo cargar el catálogo de municipios.", "error");
        });
}

function llenarPaises(idSeleccionado = "") {
    $("#cbPais").empty();
    $("#cbPais").append($("<option>").val("").text("Seleccione..."));

    catalogoPaises.forEach(item => {
        $("#cbPais").append(
            $("<option>")
                .val(item.idPais)
                .text(item.texto)
        );
    });

    if (idSeleccionado) {
        $("#cbPais").val(idSeleccionado);
    }
}

function llenarEstados(idPais = "", idSeleccionado = "") {
    $("#cbEstado").empty();
    $("#cbEstado").append($("<option>").val("").text("Seleccione..."));

    const estadosFiltrados = idPais
        ? catalogoEstados.filter(x => Number(x.idPais) === Number(idPais))
        : catalogoEstados;

    estadosFiltrados.forEach(item => {
        $("#cbEstado").append(
            $("<option>")
                .val(item.idEstado)
                .text(item.texto)
        );
    });

    if (idSeleccionado) {
        $("#cbEstado").val(idSeleccionado);
    }
}

function llenarMunicipios(idEstado = "", idSeleccionado = "") {
    $("#cbMunicipio").empty();
    $("#cbMunicipio").append($("<option>").val("").text("Seleccione..."));

    const municipiosFiltrados = idEstado
        ? catalogoMunicipios.filter(x => Number(x.idEstado) === Number(idEstado))
        : catalogoMunicipios;

    municipiosFiltrados.forEach(item => {
        $("#cbMunicipio").append(
            $("<option>")
                .val(item.idMunicipio)
                .text(item.texto)
        );
    });

    if (idSeleccionado) {
        $("#cbMunicipio").val(idSeleccionado);
    }
}

$("#cbPais").on("change", function () {
    const idPais = $(this).val();
    llenarEstados(idPais, "");
    llenarMunicipios("", "");
});

$("#cbEstado").on("change", function () {
    const idEstado = $(this).val();
    llenarMunicipios(idEstado, "");
});

function obtenerNegocio() {
    return fetch("/Negocio/Obtener")
        .then(response => response.ok ? response.json() : Promise.reject(response))
        .then(responseJson => {
            console.log("GET /Negocio/Obtener ->", responseJson);

            if (responseJson.estado) {
                const d = responseJson.objeto;

                $("#txtRfc").val(d.rfc);
                $("#txtRazonSocial").val(d.nombre);
                $("#txtCorreo").val(d.correo);
                $("#txtDireccion").val(d.direccion);
                $("#txTelefono").val(d.telefono);
                $("#txtCodigoPostal").val(d.codigoPostal);
                $("#txtSimboloMoneda").val(d.simboloMoneda);
                $("#cbRegimenFiscal").val(d.regimenFiscal);
                $("#imgLogo").attr("src", d.urlLogo);

                llenarPaises(d.idPais || "");
                llenarEstados(d.idPais || "", d.idEstado || "");
                llenarMunicipios(d.idEstado || "", d.idMunicipio || "");
            } else {
                swal("Lo sentimos", responseJson.mensaje, "error");
            }
        })
        .catch(err => {
            console.error("Error fetch Obtener:", err);
            swal("Error", "No se pudo obtener datos del negocio. Revisa consola.", "error");
        });
}

$("#btnGuardarCambios").click(function () {
    const inputs = $("input.input-validar, select.input-validar, textarea.input-validar").serializeArray();
    const inputs_sin_valor = inputs.filter((item) => item.value == null || item.value.toString().trim() == "");

    if (inputs_sin_valor.length > 0) {
        const mensaje = `Debe completar el campo: "${inputs_sin_valor[0].name}"`;
        toastr.warning("", mensaje);
        $(`[name="${inputs_sin_valor[0].name}"]`).focus();
        return;
    }

    const idPais = $("#cbPais").val();
    const idEstado = $("#cbEstado").val();
    const idMunicipio = $("#cbMunicipio").val();

    const paisSeleccionado = catalogoPaises.find(x => Number(x.idPais) === Number(idPais));
    const estadoSeleccionado = catalogoEstados.find(x => Number(x.idEstado) === Number(idEstado));
    const municipioSeleccionado = catalogoMunicipios.find(x => Number(x.idMunicipio) === Number(idMunicipio));

    const modelo = {
        rfc: $("#txtRfc").val(),
        nombre: $("#txtRazonSocial").val(),
        correo: $("#txtCorreo").val(),
        direccion: $("#txtDireccion").val(),
        telefono: $("#txTelefono").val(),
        codigoPostal: $("#txtCodigoPostal").val(),
        idPais: parseInt(idPais),
        pais: paisSeleccionado ? paisSeleccionado.descripcion : "",
        idEstado: parseInt(idEstado),
        estadoNombre: estadoSeleccionado ? estadoSeleccionado.descripcion : "",
        idMunicipio: parseInt(idMunicipio),
        municipio: municipioSeleccionado ? municipioSeleccionado.descripcion : "",
        simboloMoneda: $("#txtSimboloMoneda").val(),
        regimenFiscal: $("#cbRegimenFiscal").val()
    };

    const inputLogo = document.getElementById("txtLogo");
    const formData = new FormData();

    if (inputLogo && inputLogo.files && inputLogo.files.length > 0) {
        formData.append("logo", inputLogo.files[0]);
    }

    formData.append("modelo", JSON.stringify(modelo));

    $(".card-body").LoadingOverlay("show");

    fetch("/Negocio/GuardarCambios", {
        method: "POST",
        body: formData
    })
        .then(response => {
            $(".card-body").LoadingOverlay("hide");
            return response.ok ? response.json() : Promise.reject(response);
        })
        .then(responseJson => {
            console.log("POST /Negocio/GuardarCambios ->", responseJson);

            if (responseJson.estado) {
                const d = responseJson.objeto;

                $("#txtRfc").val(d.rfc);
                $("#txtRazonSocial").val(d.nombre);
                $("#txtCorreo").val(d.correo);
                $("#txtDireccion").val(d.direccion);
                $("#txTelefono").val(d.telefono);
                $("#txtCodigoPostal").val(d.codigoPostal);
                $("#txtSimboloMoneda").val(d.simboloMoneda);
                $("#cbRegimenFiscal").val(d.regimenFiscal);
                $("#imgLogo").attr("src", d.urlLogo);

                llenarPaises(d.idPais || "");
                llenarEstados(d.idPais || "", d.idEstado || "");
                llenarMunicipios(d.idEstado || "", d.idMunicipio || "");

                toastr.success("", "Cambios guardados correctamente");
            } else {
                swal("Lo sentimos", responseJson.mensaje, "error");
            }
        })
        .catch(err => {
            $(".card-body").LoadingOverlay("hide");
            console.error("Error POST GuardarCambios:", err);
            swal("Error", "Ocurrió un error al guardar. Revisa consola.", "error");
        });
});