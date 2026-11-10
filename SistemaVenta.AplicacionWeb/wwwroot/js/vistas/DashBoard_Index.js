let chartVentas = null;
let chartProductos = null;

$(document).ready(function () {
    limpiarDashboard();

    $("div.container-fluid").LoadingOverlay("show");

    fetch("/DashBoard/ObtenerResumen")
        .then(response => {
            $("div.container-fluid").LoadingOverlay("hide");
            return response.ok ? response.json() : Promise.reject(response);
        })
        .then(responseJson => {
            if (!responseJson.estado) {
                limpiarDashboard();
                destruirGraficas();
                swal("Lo sentimos", responseJson.mensaje || "No se pudo obtener la información del dashboard.", "error");
                return;
            }

            const d = responseJson.objeto || {};

            $("#totalVenta").text(d.totalVentas ?? 0);
            $("#totalIngresos").text(d.totalIngresos ?? "$0.00");
            $("#totalProductos").text(d.totalProductos ?? 0);
            $("#totalCategorias").text(d.totalCategorias ?? 0);

            const ventasUltimaSemana = d.ventasUltimaSemana || [];
            const productosTopUltimaSemana = d.productosTopUltimaSemana || [];

            const barchart_labels = ventasUltimaSemana.length > 0
                ? ventasUltimaSemana.map(item => item.fecha)
                : ["Sin resultados"];

            const barchart_data = ventasUltimaSemana.length > 0
                ? ventasUltimaSemana.map(item => item.total)
                : [0];

            const piechart_labels = productosTopUltimaSemana.length > 0
                ? productosTopUltimaSemana.map(item => item.producto)
                : ["Sin resultados"];

            const piechart_data = productosTopUltimaSemana.length > 0
                ? productosTopUltimaSemana.map(item => item.cantidad)
                : [0];

            destruirGraficas();

            const controlVenta = document.getElementById("chartVentas");
            chartVentas = new Chart(controlVenta, {
                type: 'bar',
                data: {
                    labels: barchart_labels,
                    datasets: [{
                        label: "Cantidad",
                        backgroundColor: "#4e73df",
                        hoverBackgroundColor: "#2e59d9",
                        borderColor: "#4e73df",
                        data: barchart_data
                    }]
                },
                options: {
                    maintainAspectRatio: false,
                    legend: {
                        display: false
                    },
                    scales: {
                        xAxes: [{
                            gridLines: {
                                display: false,
                                drawBorder: false
                            },
                            maxBarThickness: 50
                        }],
                        yAxes: [{
                            ticks: {
                                min: 0,
                                maxTicksLimit: 5
                            }
                        }]
                    }
                }
            });

            const controlProducto = document.getElementById("chartProductos");
            chartProductos = new Chart(controlProducto, {
                type: 'doughnut',
                data: {
                    labels: piechart_labels,
                    datasets: [{
                        data: piechart_data,
                        backgroundColor: ['#4e73df', '#1cc88a', '#36b9cc', "#FF785B"],
                        hoverBackgroundColor: ['#2e59d9', '#17a673', '#2c9faf', "#FF5733"],
                        hoverBorderColor: "rgba(234, 236, 244, 1)"
                    }]
                },
                options: {
                    maintainAspectRatio: false,
                    tooltips: {
                        backgroundColor: "rgb(255,255,255)",
                        bodyFontColor: "#858796",
                        borderColor: '#dddfeb',
                        borderWidth: 1,
                        xPadding: 15,
                        yPadding: 15,
                        displayColors: false,
                        caretPadding: 10
                    },
                    legend: {
                        display: true
                    },
                    cutoutPercentage: 80
                }
            });
        })
        .catch(err => {
            $("div.container-fluid").LoadingOverlay("hide");
            limpiarDashboard();
            destruirGraficas();
            console.error("Error cargando dashboard:", err);
            swal("Lo sentimos", "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.", "error");
        });
});

function limpiarDashboard() {
    $("#totalVenta").text("0");
    $("#totalIngresos").text("$0.00");
    $("#totalProductos").text("0");
    $("#totalCategorias").text("0");
}

function destruirGraficas() {
    if (chartVentas) {
        chartVentas.destroy();
        chartVentas = null;
    }

    if (chartProductos) {
        chartProductos.destroy();
        chartProductos = null;
    }
}