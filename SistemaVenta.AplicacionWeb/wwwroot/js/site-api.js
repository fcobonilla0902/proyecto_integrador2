function mostrarErrorConexion(mensaje) {
    swal("Lo sentimos", mensaje || "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.", "error");
}

function procesarRespuestaJson(responseJson, onSuccess) {
    if (responseJson && responseJson.estado) {
        onSuccess(responseJson.objeto ?? responseJson);
        return;
    }

    mostrarErrorConexion(responseJson?.mensaje || "No se pudo procesar la solicitud.");
}

function manejarErrorFetch(error, mensaje) {
    console.error(error);
    mostrarErrorConexion(mensaje || "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.");
}