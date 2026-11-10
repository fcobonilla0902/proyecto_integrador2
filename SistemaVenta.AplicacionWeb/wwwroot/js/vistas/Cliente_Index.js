const MODELO_BASE = {
    idCliente: 0,
    nombre: "",
    correo: "",
    rfc: "",
    direccionFiscal: "",
    codigoPostal: "",
    domicilioFiscalReceptor: "",
    regimenFiscalReceptor: "",
    idPais: null,
    pais: "",
    idEstado: null,
    estadoNombre: "",
    idMunicipio: null,
    municipio: "",
    esActivo: 1
};

let tablaData;
let filaSeleccionada;
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
        .then(() => inicializarTabla())
        .finally(() => $(".card-body").LoadingOverlay("hide"));
});

function cargarRegimenesFiscales() {
    return fetch("/Cliente/ListaRegimenFiscal")
        .then(response => response.ok ? response.json() : Promise.reject(response))
        .then(responseJson => {
            $("#cboRegimenFiscal").empty();
            $("#cboRegimenFiscal").append($("<option>").val("").text("Seleccione..."));

            responseJson.forEach(item => {
                $("#cboRegimenFiscal").append(
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
    return fetch("/Cliente/ListaPaises")
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
    return fetch("/Cliente/ListaEstados")
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
    return fetch("/Cliente/ListaMunicipios")
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
    $("#cboPais").empty();
    $("#cboPais").append($("<option>").val("").text("Seleccione..."));

    catalogoPaises.forEach(item => {
        $("#cboPais").append(
            $("<option>")
                .val(item.idPais)
                .text(item.texto)
        );
    });

    if (idSeleccionado) {
        $("#cboPais").val(idSeleccionado);
    }
}

function llenarEstados(idPais = "", idSeleccionado = "") {
    $("#cboEstadoSat").empty();
    $("#cboEstadoSat").append($("<option>").val("").text("Seleccione..."));

    const estadosFiltrados = idPais
        ? catalogoEstados.filter(x => Number(x.idPais) === Number(idPais))
        : catalogoEstados;

    estadosFiltrados.forEach(item => {
        $("#cboEstadoSat").append(
            $("<option>")
                .val(item.idEstado)
                .text(item.texto)
        );
    });

    if (idSeleccionado) {
        $("#cboEstadoSat").val(idSeleccionado);
    }
}

function llenarMunicipios(idEstado = "", idSeleccionado = "") {
    $("#cboMunicipio").empty();
    $("#cboMunicipio").append($("<option>").val("").text("Seleccione..."));

    const municipiosFiltrados = idEstado
        ? catalogoMunicipios.filter(x => Number(x.idEstado) === Number(idEstado))
        : catalogoMunicipios;

    municipiosFiltrados.forEach(item => {
        $("#cboMunicipio").append(
            $("<option>")
                .val(item.idMunicipio)
                .text(item.texto)
        );
    });

    if (idSeleccionado) {
        $("#cboMunicipio").val(idSeleccionado);
    }
}

$("#cboPais").on("change", function () {
    const idPais = $(this).val();
    llenarEstados(idPais, "");
    llenarMunicipios("", "");
});

$("#cboEstadoSat").on("change", function () {
    const idEstado = $(this).val();
    llenarMunicipios(idEstado, "");
});

function obtenerTextoRegimen(codigo) {
    const option = $(`#cboRegimenFiscal option[value="${codigo}"]`);
    return option.length > 0 ? option.text() : (codigo || "");
}

function inicializarTabla() {
    tablaData = $('#tbdata').DataTable({
        responsive: true,
        ajax: {
            url: '/Cliente/Lista',
            type: "GET",
            dataType: "json",
            dataSrc: function (json) {
                if (json.estado) {
                    return json.objeto || [];
                }

                swal("Lo sentimos", json.mensaje || "No se pudo cargar la lista de clientes.", "error");
                return [];
            },
            error: function () {
                swal("Lo sentimos", "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.", "error");
            }
        },
        columns: [
            { data: "idCliente", visible: false, searchable: false },
            { data: "nombre" },
            { data: "correo" },
            { data: "rfc" },
            { data: "direccionFiscal" },
            { data: "codigoPostal" },
            { data: "pais" },
            { data: "estadoNombre" },
            { data: "municipio" },
            {
                data: "regimenFiscalReceptor",
                render: function (data) {
                    return obtenerTextoRegimen(data);
                }
            },
            {
                data: "esActivo",
                render: function (data) {
                    if (data == 1)
                        return '<span class="badge badge-info">Activo</span>';
                    else
                        return '<span class="badge badge-danger">No Activo</span>';
                }
            },
            {
                defaultContent: '<button class="btn btn-primary btn-editar btn-sm mr-2"><i class="fas fa-pencil-alt"></i></button>' +
                    '<button class="btn btn-danger btn-eliminar btn-sm"><i class="fas fa-trash-alt"></i></button>',
                orderable: false,
                searchable: false,
                width: "80px"
            }
        ],
        order: [[0, "asc"]],
        dom: "Bfrtip",
        buttons: [
            {
                text: 'Exportar Excel',
                extend: 'excelHtml5',
                title: '',
                filename: 'Reporte Clientes',
                exportOptions: {
                    columns: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
                }
            },
            'pageLength'
        ],
        language: {
            url: "https://cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json"
        }
    });
}

function mostrarModal(modelo = MODELO_BASE) {
    $("#txtId").val(modelo.idCliente);
    $("#txtNombre").val(modelo.nombre);
    $("#txtCorreo").val(modelo.correo);
    $("#txtRFC").val(modelo.rfc);
    $("#txtDireccionFiscal").val(modelo.direccionFiscal);
    $("#txtCodigoPostal").val(modelo.codigoPostal || modelo.domicilioFiscalReceptor);
    $("#clienteEstado").val(modelo.esActivo ?? 1);
    $("#cboRegimenFiscal").val(modelo.regimenFiscalReceptor);

    llenarPaises(modelo.idPais || "");
    llenarEstados(modelo.idPais || "", modelo.idEstado || "");
    llenarMunicipios(modelo.idEstado || "", modelo.idMunicipio || "");

    $("#modalData").modal("show");
}

$("#btnNuevo").click(function () {
    mostrarModal();
});

$("#btnGuardar").click(function () {
    const inputs = $("input.input-validar, select.input-validar").serializeArray();
    const inputs_sin_valor = inputs.filter((item) => item.value == null || item.value.toString().trim() == "");

    if (inputs_sin_valor.length > 0) {
        const mensaje = `Debe completar el campo: "${inputs_sin_valor[0].name}"`;
        toastr.warning("", mensaje);
        $(`[name="${inputs_sin_valor[0].name}"]`).focus();
        return;
    }

    const modelo = structuredClone(MODELO_BASE);

    modelo.idCliente = parseInt($("#txtId").val());
    modelo.nombre = $("#txtNombre").val();
    modelo.correo = $("#txtCorreo").val();
    modelo.rfc = $("#txtRFC").val();
    modelo.direccionFiscal = $("#txtDireccionFiscal").val();
    modelo.codigoPostal = $("#txtCodigoPostal").val();
    modelo.domicilioFiscalReceptor = $("#txtCodigoPostal").val();
    modelo.idPais = parseInt($("#cboPais").val());
    modelo.idEstado = parseInt($("#cboEstadoSat").val());
    modelo.idMunicipio = parseInt($("#cboMunicipio").val());
    modelo.regimenFiscalReceptor = $("#cboRegimenFiscal").val();
    modelo.esActivo = parseInt($("#clienteEstado").val());

    const formData = new FormData();
    formData.append("modelo", JSON.stringify(modelo));

    $("#modalData").find("div.modal-content").LoadingOverlay("show");

    if (modelo.idCliente == 0) {
        fetch("/Cliente/Crear", {
            method: "POST",
            body: formData
        })
            .then(response => {
                $("#modalData").find("div.modal-content").LoadingOverlay("hide");
                return response.ok ? response.json() : Promise.reject(response);
            })
            .then(responseJson => {
                if (responseJson.estado) {
                    tablaData.row.add(responseJson.objeto).draw(false);
                    $("#modalData").modal("hide");
                    swal("Listo!", "El cliente fue creado", "success");
                } else {
                    swal("Lo sentimos", responseJson.mensaje, "error");
                }
            })
            .catch(err => {
                $("#modalData").find("div.modal-content").LoadingOverlay("hide");
                console.error("Error creando cliente:", err);
                swal("Lo sentimos", "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.", "error");
            });
    } else {
        fetch("/Cliente/Editar", {
            method: "PUT",
            body: formData
        })
            .then(response => {
                $("#modalData").find("div.modal-content").LoadingOverlay("hide");
                return response.ok ? response.json() : Promise.reject(response);
            })
            .then(responseJson => {
                if (responseJson.estado) {
                    tablaData.row(filaSeleccionada).data(responseJson.objeto).draw(false);
                    filaSeleccionada = null;
                    $("#modalData").modal("hide");
                    swal("Listo!", "El cliente fue modificado", "success");
                } else {
                    swal("Lo sentimos", responseJson.mensaje, "error");
                }
            })
            .catch(err => {
                $("#modalData").find("div.modal-content").LoadingOverlay("hide");
                console.error("Error creando cliente:", err);
                swal("Lo sentimos", "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.", "error");
            });
    }
});

$("#tbdata tbody").on("click", ".btn-editar", function () {
    if ($(this).closest("tr").hasClass("child")) {
        filaSeleccionada = $(this).closest("tr").prev();
    } else {
        filaSeleccionada = $(this).closest("tr");
    }

    const data = tablaData.row(filaSeleccionada).data();
    mostrarModal(data);
});

$("#tbdata tbody").on("click", ".btn-eliminar", function () {
    let fila;

    if ($(this).closest("tr").hasClass("child")) {
        fila = $(this).closest("tr").prev();
    } else {
        fila = $(this).closest("tr");
    }

    const data = tablaData.row(fila).data();

    swal({
        title: "¿Está seguro?",
        text: `Eliminar al cliente "${data.nombre}"`,
        type: "warning",
        showCancelButton: true,
        confirmButtonClass: "btn-danger",
        confirmButtonText: "Sí, eliminar",
        cancelButtonText: "No, cancelar",
        closeOnConfirm: false,
        closeOnCancel: true
    },
        function (respuesta) {
            if (respuesta) {
                $(".showSweetAlert").LoadingOverlay("show");

                fetch(`/Cliente/Eliminar?idCLiente=${data.idCliente}`, {
                    method: "DELETE"
                })
                    .then(response => {
                        $(".showSweetAlert").LoadingOverlay("hide");
                        return response.ok ? response.json() : Promise.reject(response);
                    })
                    .then(responseJson => {
                        if (responseJson.estado) {
                            tablaData.row(fila).remove().draw();
                            swal("Listo!", "El cliente fue eliminado", "success");
                        } else {
                            swal("Lo sentimos", responseJson.mensaje, "error");
                        }
                    })
                    .catch(err => {
                        $(".showSweetAlert").LoadingOverlay("hide");
                        console.error("Error eliminando cliente:", err);
                        swal("Lo sentimos", "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.", "error");
                    });
            }
        }
    );
});