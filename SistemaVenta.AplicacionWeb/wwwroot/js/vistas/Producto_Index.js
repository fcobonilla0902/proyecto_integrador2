const MODELO_BASE = {
    idProducto: 0,
    codigoBarra: "",
    marca: "",
    modelo: "",
    descripcion: "",
    idCategoria: 0,
    nombreCategoria: "",
    stock: 0,
    urlImagen: "",
    nombreImagen: "",
    precio: "0.00",
    esActivo: 1,
    medidaEmpresa: "0",
    medidaSat: "0",
    claveProductoSat: "0",
    objetoImpuesto: "0",
    impuesto: "0",
    factorImpuesto: "0",
    valorImpuesto: 0.0,
    descuento: 0.0,
    idMarca: 0,
    idModelo: 0,
    idMedidaLocal: 0,
    idClaveUnidadSAT: 0,
    idClaveProdServSAT: 0,
    idObjetoImpuestoSAT: 0,
    idImpuestoSAT: 0,
    idTipoFactorSAT: 0,
    idMoneda: 0
};

const MARCAS = [
    { idMarca: 1, c_Marca: "HALion", descripcion: "HALION INTERNACIONAL S.A.", idStatusRegistro: 1 },
    { idMarca: 2, c_Marca: "INTEL", descripcion: "Intel Corporation", idStatusRegistro: 1 },
    { idMarca: 3, c_Marca: "ASUS", descripcion: "ASUSTeK Computer Inc.", idStatusRegistro: 1 },
    { idMarca: 4, c_Marca: "HUAWEI", descripcion: "Huawei Device Co., Ltd.", idStatusRegistro: 1 },
    { idMarca: 5, c_Marca: "REDRAGON", descripcion: "Redragon", idStatusRegistro: 1 },
    { idMarca: 6, c_Marca: "CYBERTEL", descripcion: "Cibertel", idStatusRegistro: 1 },
    { idMarca: 7, c_Marca: "SAMSUNG", descripcion: "Samsung", idStatusRegistro: 1 },
    { idMarca: 8, c_Marca: "GIGABYTE", descripcion: "GIGABYTE Technology Co., Ltd.", idStatusRegistro: 1 },
    { idMarca: 9, c_Marca: "HYPERX", descripcion: "Hyperx HP Development Company, L.P", idStatusRegistro: 1 },
    { idMarca: 10, c_Marca: "TRUST", descripcion: "Trust International B.V", idStatusRegistro: 1 },
    { idMarca: 11, c_Marca: "DELL", descripcion: "Dell Inc.", idStatusRegistro: 1 },
    { idMarca: 12, c_Marca: "MICROSOFT", descripcion: "Microsoft", idStatusRegistro: 1 }
];

const MODELOS = [
    { idModelo: 1, c_Modelo: "PCCOREI5", idMarca: 1, idStatusRegistro: 1 },
    { idModelo: 2, c_Modelo: "PCCOREI58RAM", idMarca: 2, idStatusRegistro: 1 },
    { idModelo: 3, c_Modelo: "ROGZEPHYRUS", idMarca: 3, idStatusRegistro: 1 },
    { idModelo: 4, c_Modelo: "MATEBOOKD15", idMarca: 4, idStatusRegistro: 1 },
    { idModelo: 5, c_Modelo: "TCGK12", idMarca: 5, idStatusRegistro: 1 },
    { idModelo: 6, c_Modelo: "TCGGK100", idMarca: 6, idStatusRegistro: 1 },
    { idModelo: 7, c_Modelo: "MONG165", idMarca: 7, idStatusRegistro: 1 },
    { idModelo: 8, c_Modelo: "G27165", idMarca: 8, idStatusRegistro: 1 },
    { idModelo: 9, c_Modelo: "QSPC", idMarca: 9, idStatusRegistro: 1 },
    { idModelo: 10, c_Modelo: "MICPC", idMarca: 10, idStatusRegistro: 1 }
];

let tablaData;
let filaSeleccionada;

function esperarCombo(selector) {
    return new Promise(resolve => {
        const intervalo = setInterval(() => {
            if ($(selector).length && $(selector + " option").length > 0) {
                clearInterval(intervalo);
                resolve();
            }
        }, 100);
    });
}

function cargarMarcas(idMarcaSeleccionada = 0) {
    $("#cboMarca").empty().append(
        $("<option>").val(0).text("Seleccionar")
    );

    MARCAS
        .filter(m => m.idStatusRegistro === 1)
        .forEach(marca => {
            $("#cboMarca").append(
                $("<option>").val(marca.idMarca).text(marca.c_Marca)
            );
        });

    $("#cboMarca").val(String(idMarcaSeleccionada || 0));
}

function cargarModelos(idMarca, idModeloSeleccionado = 0) {
    $("#cboModelo").empty().append(
        $("<option>").val(0).text("Seleccionar")
    );

    const modelosFiltrados = MODELOS.filter(m =>
        m.idMarca === idMarca && m.idStatusRegistro === 1
    );

    modelosFiltrados.forEach(modelo => {
        $("#cboModelo").append(
            $("<option>").val(modelo.idModelo).text(modelo.c_Modelo)
        );
    });

    $("#cboModelo").val(String(idModeloSeleccionado || 0));
}

$(document).ready(function () {

    cargarMarcas();

    fetch("/Categoria/Lista")
        .then(response => response.ok ? response.json() : Promise.reject(response))
        .then(responseJson => {
            $("#cboCategoria").empty().append(
                $("<option>").val(0).text("Seleccionar")
            );

            if (responseJson.estado && responseJson.objeto && responseJson.objeto.length > 0) {
                responseJson.objeto.forEach(item => {
                    $("#cboCategoria").append(
                        $("<option>").val(item.idCategoria).text(item.descripcion)
                    );
                });
            } else if (!responseJson.estado) {
                swal("Lo sentimos", responseJson.mensaje || "No se pudo cargar la lista de categorías.", "error");
            }
        })
        .catch(() => {
            swal("Lo sentimos", "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.", "error");
        });

    $("#cboMarca").on("change", function () {
        const idMarca = parseInt($(this).val()) || 0;
        cargarModelos(idMarca, 0);
    });

    tablaData = $('#tbdata').DataTable({
        responsive: true,
        ajax: {
            url: '/Producto/Lista',
            type: "GET",
            datatype: "json",
            dataSrc: function (json) {
                if (json.estado) {
                    return json.objeto || [];
                }

                swal("Lo sentimos", json.mensaje || "No se pudo cargar la lista de productos.", "error");
                return [];
            },
            error: function () {
                swal("Lo sentimos", "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.", "error");
            }
        },
        columns: [
            { data: "idProducto", visible: false },
            {
                data: "urlImagen",
                render: function (data) {
                    const src = data && data.trim() !== "" ? data : "";
                    return `<img style="height:60px" src="${src}" class="rounded mx-auto d-block"/>`;
                }
            },
            { data: "codigoBarra" },
            { data: "marca" },
            { data: "descripcion" },
            { data: "nombreCategoria" },
            { data: "stock" },
            { data: "precio" },
            {
                data: "esActivo",
                render: function (data) {
                    return parseInt(data) === 1
                        ? '<span class="badge badge-info">Activo</span>'
                        : '<span class="badge badge-danger">No Activo</span>';
                }
            },
            {
                defaultContent:
                    '<button class="btn btn-primary btn-editar btn-sm mr-2"><i class="fas fa-pencil-alt"></i></button>' +
                    '<button class="btn btn-danger btn-eliminar btn-sm"><i class="fas fa-trash-alt"></i></button>',
                orderable: false,
                searchable: false,
                width: "80px"
            }
        ],
        order: [[0, "desc"]],
        dom: "Bfrtip",
        buttons: [
            {
                text: 'Exportar Excel',
                extend: 'excelHtml5',
                filename: 'Reporte Productos',
                exportOptions: { columns: [2, 3, 4, 5, 6, 7] }
            },
            'pageLength'
        ],
        language: { url: "https://cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json" }
    });
});

function mostrarModal(modelo) {
    const esNuevo = !modelo || !modelo.idProducto || modelo.idProducto === 0;

    if (esNuevo) {
        modelo = structuredClone(MODELO_BASE);
    }

    $("#txtId").val(modelo.idProducto || 0);
    $("#txtCodigoBarra").val(modelo.codigoBarra || "");
    $("#txtDescripcion").val(modelo.descripcion || "");
    $("#cboCategoria").val(esNuevo ? "0" : String(modelo.idCategoria ?? "0"));
    $("#txtStock").val(modelo.stock ?? 0);
    $("#txtPrecio").val(modelo.precio ?? "0.00");
    $("#cboEstado").val(String(modelo.esActivo ?? 1));
    $("#cboMoneda").val(String(modelo.idMoneda || 0));

    $("#txtImagen").val("");
    $("#imgProducto")
        .attr("src", esNuevo ? "" : (modelo.urlImagen || ""))
        .data("nombreimagen", esNuevo ? "" : (modelo.nombreImagen || ""));

    cargarMarcas(modelo.idMarca || 0);
    cargarModelos(modelo.idMarca || 0, modelo.idModelo || 0);

    Promise.all([
        esperarCombo("#cboMarca"),
        esperarCombo("#cboModelo"),
        esperarCombo("#cboMedidaEmpresa"),
        esperarCombo("#cboMedidaSat"),
        esperarCombo("#cboClaveProductoSat"),
        esperarCombo("#cboObjetoImpuesto"),
        esperarCombo("#cboImpuesto"),
        esperarCombo("#cboFactorImpuesto"),
        esperarCombo("#cboMoneda")
    ]).then(() => {
        $("#cboMarca").val(String(modelo.idMarca || 0));
        $("#cboModelo").val(String(modelo.idModelo || 0));
        $("#cboMedidaEmpresa").val(modelo.medidaEmpresa || "0");
        $("#cboMedidaSat").val(String(modelo.idClaveUnidadSAT || 0));
        $("#cboMoneda").val(String(modelo.idMoneda || 0));

        const claveProdMap = {
            1: "01010101",
            24115: "43211503",
            24119: "43211507",
            24036: "43201803",
            17326: "32101601",
            25430: "43211900",
            24168: "43222609",
            24156: "43191501",
            24158: "43211708"
        };

        const objetoImpMap = { 1: "01", 2: "02", 3: "03", 4: "04" };
        const impuestoMap = { 1: "001", 2: "002", 3: "003" };
        const factorMap = { 1: "Tasa", 2: "Cuota", 3: "Exento" };

        $("#cboClaveProductoSat").val(claveProdMap[modelo.idClaveProdServSAT] || "0");
        $("#cboObjetoImpuesto").val(objetoImpMap[modelo.idObjetoImpuestoSAT] || "0");
        $("#cboImpuesto").val(impuestoMap[modelo.idImpuestoSAT] || "0");
        $("#cboFactorImpuesto").val(factorMap[modelo.idTipoFactorSAT] || "0");
        $("#decValorImpuesto").val(modelo.valorImpuesto ?? "");
        $("#decDescuento").val(modelo.descuento ?? "");
    });

    $("#modalData").modal("show");
}

$("#btnNuevo").click(function () {
    mostrarModal(null);
});

$("#btnGuardar").click(function () {

    const camposRequeridos = [
        { selector: "#txtCodigoBarra", tipo: "input", nombre: "Código Barra" },
        { selector: "#cboMarca", tipo: "select", nombre: "Marca" },
        { selector: "#cboModelo", tipo: "select", nombre: "Modelo" },
        { selector: "#txtDescripcion", tipo: "input", nombre: "Descripción" },
        { selector: "#txtStock", tipo: "input", nombre: "Stock" },
        { selector: "#txtPrecio", tipo: "input", nombre: "Precio Unitario" },
        { selector: "#decValorImpuesto", tipo: "input", nombre: "Valor Impuesto" },
        { selector: "#cboMedidaEmpresa", tipo: "select", nombre: "Medida de Empresa" },
        { selector: "#cboMedidaSat", tipo: "select", nombre: "Medida del SAT" },
        { selector: "#cboCategoria", tipo: "select", nombre: "Categoría" },
        { selector: "#cboClaveProductoSat", tipo: "select", nombre: "Clave Producto SAT" },
        { selector: "#cboObjetoImpuesto", tipo: "select", nombre: "Objeto del Impuesto" },
        { selector: "#cboImpuesto", tipo: "select", nombre: "Impuesto" },
        { selector: "#cboFactorImpuesto", tipo: "select", nombre: "Factor del Impuesto" }
    ];

    for (const c of camposRequeridos) {
        const $campo = $(c.selector);
        const val = $campo.val();

        if (c.tipo === "input") {
            if (!val || val.toString().trim() === "") {
                toastr.warning("", `Debe completar el campo "${c.nombre}"`);
                $campo.focus();
                return;
            }
        } else {
            if (val === "0" || val === null || val === "") {
                toastr.warning("", `Debe seleccionar "${c.nombre}"`);
                $campo.focus();
                return;
            }
        }
    }

    const modelo = structuredClone(MODELO_BASE);

    modelo.idProducto = parseInt($("#txtId").val()) || 0;
    modelo.codigoBarra = $("#txtCodigoBarra").val().trim();
    modelo.idMarca = parseInt($("#cboMarca").val()) || 0;
    modelo.marca = $("#cboMarca option:selected").text();
    modelo.idModelo = parseInt($("#cboModelo").val()) || 0;
    modelo.modelo = $("#cboModelo option:selected").text();
    modelo.descripcion = $("#txtDescripcion").val().trim();
    modelo.idCategoria = parseInt($("#cboCategoria").val()) || 0;
    modelo.stock = parseInt($("#txtStock").val()) || 0;
    modelo.precio = ($("#txtPrecio").val() || "0").toString().trim();
    modelo.esActivo = parseInt($("#cboEstado").val()) || 0;
    modelo.valorImpuesto = parseFloat($("#decValorImpuesto").val()) || 0;
    modelo.descuento = parseFloat($("#decDescuento").val()) || 0;
    modelo.idMoneda = parseInt($("#cboMoneda").val()) || 0;

    modelo.medidaEmpresa = $("#cboMedidaEmpresa").val();
    modelo.medidaSat = $("#cboMedidaSat option:selected").text();
    modelo.claveProductoSat = $("#cboClaveProductoSat").val();
    modelo.objetoImpuesto = $("#cboObjetoImpuesto").val();
    modelo.impuesto = $("#cboImpuesto").val();
    modelo.factorImpuesto = $("#cboFactorImpuesto").val();

    modelo.idMedidaLocal = {
        "PZA": 1,
        "XBX": 2,
        "BG": 3,
        "RO": 4,
        "LTR": 5,
        "MTR": 6,
        "KGM": 7,
        "GRM": 8
    }[$("#cboMedidaEmpresa").val()] || 0;

    modelo.idClaveUnidadSAT = parseInt($("#cboMedidaSat").val()) || 0;

    modelo.idClaveProdServSAT = {
        "43211507": 24119,
        "43211503": 24115,
        "43191501": 24156,
        "43211900": 25430,
        "43222609": 24168,
        "01010101": 1,
        "43201803": 24036,
        "32101601": 17326,
        "43211708": 24158
    }[$("#cboClaveProductoSat").val()] || 0;

    modelo.idObjetoImpuestoSAT = {
        "01": 1,
        "02": 2,
        "03": 3,
        "04": 4
    }[$("#cboObjetoImpuesto").val()] || 0;

    modelo.idImpuestoSAT = {
        "001": 1,
        "002": 2,
        "003": 3
    }[$("#cboImpuesto").val()] || 0;

    modelo.idTipoFactorSAT = {
        "Tasa": 1,
        "Cuota": 2,
        "Exento": 3
    }[$("#cboFactorImpuesto").val()] || 0;

    const inputFoto = document.getElementById("txtImagen");
    const formData = new FormData();

    if (inputFoto.files.length > 0) {
        formData.append("imagen", inputFoto.files[0]);
    }

    modelo.urlImagen = $("#imgProducto").attr("src") || "";
    modelo.nombreImagen = $("#imgProducto").data("nombreimagen") || "";
    formData.append("modelo", JSON.stringify(modelo));

    $("#modalData").find("div.modal-content").LoadingOverlay("show");

    const url = modelo.idProducto === 0 ? "/Producto/Crear" : "/Producto/Editar";
    const method = modelo.idProducto === 0 ? "POST" : "PUT";

    fetch(url, {
        method: method,
        body: formData
    })
        .then(response => {
            $("#modalData").find("div.modal-content").LoadingOverlay("hide");
            return response.ok ? response.json() : Promise.reject(response);
        })
        .then(responseJson => {
            if (responseJson.estado) {
                if (modelo.idProducto === 0) {
                    tablaData.row.add(responseJson.objeto).draw(false);
                } else {
                    tablaData.row(filaSeleccionada).data(responseJson.objeto).draw(false);
                    filaSeleccionada = null;
                }

                $("#modalData").modal("hide");
                swal("Listo!", modelo.idProducto === 0 ? "El producto fue creado" : "El producto fue modificado", "success");
            } else {
                swal("Lo sentimos", responseJson.mensaje, "error");
            }
        })
        .catch(err => {
            $("#modalData").find("div.modal-content").LoadingOverlay("hide");
            console.error(err);
            swal("Lo sentimos", "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.", "error");
        });
});

$("#tbdata tbody").on("click", ".btn-editar", function () {
    filaSeleccionada = $(this).closest("tr").hasClass("child")
        ? $(this).closest("tr").prev()
        : $(this).closest("tr");

    const data = tablaData.row(filaSeleccionada).data();
    mostrarModal(data);
});

$("#tbdata tbody").on("click", ".btn-eliminar", function () {
    let fila = $(this).closest("tr").hasClass("child")
        ? $(this).closest("tr").prev()
        : $(this).closest("tr");

    const data = tablaData.row(fila).data();

    swal({
        title: "¿Está seguro?",
        text: `Eliminar el producto "${data.descripcion}"`,
        type: "warning",
        showCancelButton: true,
        confirmButtonClass: "btn-danger",
        confirmButtonText: "Sí, eliminar",
        cancelButtonText: "Cancelar",
        closeOnConfirm: false,
        closeOnCancel: true
    }, function (respuesta) {
        if (respuesta) {
            $(".showSweetAlert").LoadingOverlay("show");

            fetch(`/Producto/Eliminar?IdProducto=${data.idProducto}`, {
                method: "DELETE"
            })
                .then(response => {
                    $(".showSweetAlert").LoadingOverlay("hide");
                    return response.ok ? response.json() : Promise.reject(response);
                })
                .then(responseJson => {
                    if (responseJson.estado) {
                        tablaData.row(fila).remove().draw();
                        swal("Listo!", "El producto fue eliminado", "success");
                    } else {
                        swal("Lo sentimos", responseJson.mensaje, "error");
                    }
                })
                .catch(err => {
                    $(".showSweetAlert").LoadingOverlay("hide");
                    console.error(err);
                    swal("Lo sentimos", "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.", "error");
                });
        }
    });
});