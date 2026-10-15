
const API_URL = "http://localhost:3000/api/avisos";

const listaAvisos = document.getElementById("listaAvisos");
const filtroRiesgo = document.getElementById("filtroRiesgo");
const mensaje = document.getElementById("mensaje");

let avisosRecibidos = [];

async function cargarAvisos() {
    const conexion = document.getElementById("estadoConexion");

    try {
        const respuesta = await fetch(API_URL);

        if (!respuesta.ok) {
            throw new Error("La API devolvió un error.");
        }

        const datos = await respuesta.json();

        // La API debe devolver una lista de avisos.
        if (!Array.isArray(datos)) {
            throw new Error("La respuesta de la API no es una lista.");
        }

        avisosRecibidos = datos;

        conexion.textContent = "Conectado a la API";
        mensaje.textContent = "";

        actualizarResumen();
        mostrarAvisos();

        document.getElementById("ultimaConsulta").textContent =
            new Date().toLocaleTimeString("es-AR");

    } catch (error) {
        console.error("Error al consultar los avisos:", error);

        conexion.textContent = "Sin conexión";
        mensaje.textContent =
            "No se pudieron cargar los avisos. Verificá el servidor y la API.";
    }
}

function actualizarResumen() {
    document.getElementById("totalAvisos").textContent =
        avisosRecibidos.length;

    document.getElementById("totalAltos").textContent =
        avisosRecibidos.filter(a => normalizar(a.nivel) === "alto").length;

    document.getElementById("totalMedios").textContent =
        avisosRecibidos.filter(a => normalizar(a.nivel) === "medio").length;

    document.getElementById("totalBajos").textContent =
        avisosRecibidos.filter(a => normalizar(a.nivel) === "bajo").length;
}

function normalizar(valor) {
    return String(valor ?? "").trim().toLowerCase();
}

function mostrarAvisos() {
    const filtro = filtroRiesgo.value;

    const filtrados = avisosRecibidos.filter(aviso => {
        return filtro === "todos" ||
            normalizar(aviso.nivel) === filtro;
    });

    listaAvisos.replaceChildren();

    if (filtrados.length === 0) {
        const fila = document.createElement("tr");
        const celda = document.createElement("td");

        celda.colSpan = 4;
        celda.textContent = "No hay avisos para mostrar.";

        fila.appendChild(celda);
        listaAvisos.appendChild(fila);
        return;
    }

    filtrados.forEach(aviso => {
        const fila = document.createElement("tr");

        const celdaEstado = document.createElement("td");
        const etiqueta = document.createElement("span");
        const nivel = normalizar(aviso.nivel);

        etiqueta.className = "estado " +
            (["alto", "medio", "bajo"].includes(nivel) ? nivel : "");

        etiqueta.textContent = nivel
            ? nivel.toUpperCase()
            : "SIN DATOS";

        celdaEstado.appendChild(etiqueta);

        const celdaSituacion = document.createElement("td");
        celdaSituacion.textContent = aviso.tipo || "Sin especificar";

        const celdaUbicacion = document.createElement("td");
        celdaUbicacion.textContent = aviso.ubicacion || "Sin ubicación";

        const celdaFecha = document.createElement("td");
        celdaFecha.textContent =
            aviso.fecha || aviso.actualizado || "Sin fecha";

        fila.append(
            celdaEstado,
            celdaSituacion,
            celdaUbicacion,
            celdaFecha
        );

        listaAvisos.appendChild(fila);
    });
}

filtroRiesgo.addEventListener("change", mostrarAvisos);

// Consultar al abrir y actualizar cada 15 segundos.
cargarAvisos();
setInterval(cargarAvisos, 15000);

