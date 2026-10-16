const NODOS = [
    {
        id: 'NODE-01',
        name: 'Sirena y Baliza Termas de Copahue',
        location: 'Sector Norte',
        state: 'active',
        icon: 'fa-bullhorn',
        desc: 'Sirena acústica de evacuación y baliza estroboscópica'
    },
    {
        id: 'NODE-02',
        name: 'Sensor de gases cráter el agrio',
        location: 'Cráter Volcán Copahue',
        state: 'active',
        icon: 'fa-volcano',
        desc: 'Sensor óptico DOAS'
    },
    {
        id: 'NODE-03',
        name: 'Sismógrafo SEGEMAR / OVDAS',
        location: 'Flanco Este',
        state: 'active',
        icon: 'fa-chart-simple',
        desc: 'Estación sismológica triaxial de banda ancha'
    },
    {
        id: 'NODE-04',
        name: 'Repetidora Cerro Mellizo & Kit Solar',
        location: 'Cerro Mellizo',
        state: 'active',
        icon: 'fa-solar-panel',
        desc: 'Enlace de telemetría VHF/LoRa con batería al 98%'
    },
    {
        id: 'NODE-05',
        name: 'Estación Meteorológica Caviahue',
        location: 'Villa Caviahue',
        state: 'offline',
        icon: 'fa-wind',
        desc: 'Anemómetro y sensor de dirección de viento y ceniza'
    },
    {
        id: 'NODE-06',
        name: 'Sensor Térmico Laguna Cráter',
        location: 'Laguna Ácida',
        state: 'error',
        icon: 'fa-triangle-exclamation',
        desc: 'Falla de enlace por corrosión de gases fumarólicos'
    }
];

const ALERTAS_INICIALES = [
    {
        id: 1,
        time: '14:30:15',
        isoDate: '2026-10-15T14:30:15.000Z',
        title: 'Erupción inminente con pluma eruptiva',
        source: 'Sensor cráter el agrio',
        origin_type: 'simulated',
        level: 'critical',
        status: 'Pendiente',
        channels: ['Sirena Acústica', 'Baliza Roja', 'SMS Defensa Civil'],
        metadata: {
            alert_evaluated: true,
            node_id: 'NODE-02',
            trace_id: 'SIM-TRC-001'
        }
    },
    {
        id: 2,
        time: '14:22:00',
        isoDate: '2026-10-15T14:22:00.000Z',
        title: 'Tremor armónico continuo y sismicidad en flanco este',
        source: 'Estación Sísmica OVDAS / SEGEMAR',
        origin_type: 'simulated',
        level: 'high',
        status: 'Confirmado',
        channels: ['Radio VHF', 'Aviso Digital', 'Red Caviahue'],
        metadata: {
            alert_evaluated: true,
            node_id: 'NODE-03',
            trace_id: 'SIM-TRC-002'
        }
    },
    {
        id: 3,
        time: '14:15:42',
        isoDate: '2026-10-15T14:15:42.000Z',
        title: 'Lectura térmica anómala sin confirmación de sensor secundario',
        source: 'Sensor Térmico Laguna Cráter',
        origin_type: 'test',
        level: 'medium',
        status: 'Pendiente',

        channels: ['Telemetría Interna'],
        metadata: {
            alert_evaluated: false,
            node_id: 'NODE-06',
            trace_id: 'TEST-TRC-003'
        }
    },
    {
        id: 4,
        time: '13:50:10',
        isoDate: '2026-10-15T13:50:10.000Z',
        title: 'Viento leve dirección NE sin afectación urbana',
        source: 'Estación Meteorológica Caviahue',
        origin_type: 'simulated',
        level: 'low',
        status: 'Atendido',
        channels: ['Canal Meteo'],
        metadata: {
            alert_evaluated: true,
            node_id: 'NODE-05',
            trace_id: 'SIM-TRC-004'
        }
    },
    {
        id: 5,
        time: '13:30:00',
        isoDate: '2026-10-15T13:30:00.000Z',
        title: 'Monitoreo rutinario fumarólico dentro de parámetros basales',
        source: 'Sirena y Baliza Termas de Copahue',
        origin_type: 'simulated',
        level: 'normal',
        status: 'Atendido',
        channels: ['Reporte Diario'],
        metadata: {
            alert_evaluated: true,
            node_id: 'NODE-01',
            trace_id: 'SIM-TRC-005'
        }
    }
];

// Cargar alertas desde LocalStorage
let alertsData = [];
const localAlerts = localStorage.getItem('copahue_alerts_v2');
if (localAlerts) {
    try {
        alertsData = JSON.parse(localAlerts);
    } catch (e) {
        alertsData = [...ALERTAS_INICIALES];
    }
} else {
    alertsData = [...ALERTAS_INICIALES];
}

// Limpiar cualquier prefijo [SIMULACIÓN] y sufijo >3000m residual guardado en el navegador
alertsData = alertsData.map(alert => ({
    ...alert,
    title: String(alert.title || '')
        .replace(/\[SIMULACI[ÓO]N\]\s*/gi, '')
        .replace(/>\s*3000\s*m/gi, '')
        .trim()
}));
localStorage.setItem('copahue_alerts_v2', JSON.stringify(alertsData));

function normalizeLevel(level) {
    const l = String(level ?? "").trim().toLowerCase();
    if (l === 'crítico' || l === 'critico' || l === 'critical') return 'critical';
    if (l === 'alto' || l === 'high') return 'high';
    if (l === 'medio' || l === 'medium') return 'medium';
    if (l === 'bajo' || l === 'low') return 'low';
    return 'normal';
}

function renderRiskBadgeHTML(level, isEvaluated = true) {
    const norm = normalizeLevel(level);
    let iconClass = 'fa-circle-check';
    let text = 'Normal';
    let riskClass = 'risk-normal';

    switch (norm) {
        case 'critical':
            iconClass = 'fa-radiation';
            text = 'Crítico';
            riskClass = 'risk-critical';
            break;
        case 'high':
            iconClass = 'fa-circle-exclamation';
            text = 'Alto';
            riskClass = 'risk-high';
            break;
        case 'medium':
            iconClass = 'fa-triangle-exclamation';
            text = 'Medio';
            riskClass = 'risk-medium';
            break;
        case 'low':
            iconClass = 'fa-circle-info';
            text = 'Bajo';
            riskClass = 'risk-low';
            break;
        default:
            iconClass = 'fa-circle-check';
            text = 'Normal';
            riskClass = 'risk-normal';
    }

    let html = `<span class="badge-risk ${riskClass}"><i class="fa-solid ${iconClass}"></i> ${text}</span>`;
    if (isEvaluated === false) {
        html += `<div class="badge-unevaluated"><i class="fa-solid fa-triangle-exclamation"></i> Riesgo no evaluado</div>`;
    }
    return html;
}

function renderOriginBadgeHTML(originType) {
    const type = String(originType || 'simulated').toLowerCase();
    if (type === 'real') {
        return `<span class="badge-origin real"><i class="fa-solid fa-satellite-dish"></i> Real</span>`;
    }
    if (type === 'test') {
        return `<span class="badge-origin test"><i class="fa-solid fa-vial"></i> Test</span>`;
    }
    return `<span class="badge-origin simulated"><i class="fa-solid fa-flask"></i> Simulado</span>`;
}

function renderChannelsHTML(channels) {
    if (!Array.isArray(channels) || channels.length === 0) {
        return `<span class="channel-tag"><i class="fa-solid fa-bell"></i> Canal estándar</span>`;
    }
    return `
        <div class="channels-list">
            ${channels.map(ch => {
        let icon = 'fa-bullhorn';
        const lower = ch.toLowerCase();
        if (lower.includes('baliza') || lower.includes('luz')) icon = 'fa-lightbulb';
        else if (lower.includes('radio') || lower.includes('vhf')) icon = 'fa-tower-broadcast';
        else if (lower.includes('sms') || lower.includes('push') || lower.includes('cel')) icon = 'fa-mobile-screen';
        else if (lower.includes('segemar') || lower.includes('defensa') || lower.includes('red')) icon = 'fa-shield-halved';
        return `<span class="channel-tag"><i class="fa-solid ${icon}"></i> ${ch}</span>`;
    }).join('')}
        </div>
    `;
}

// Renderiza los Nodos de Telemetría (Panel index.html)
function renderNodesUI() {
    const nodesContainer = document.getElementById('nodes-container');
    if (!nodesContainer) return;

    nodesContainer.innerHTML = NODOS.map(node => {
        let stateText = 'Activo';
        if (node.state === 'offline') stateText = 'Desconectado';
        else if (node.state === 'error') stateText = 'Falla';

        return `
            <div class="node-card">
                <div class="node-info">
                    <div class="node-icon">
                        <i class="fa-solid ${node.icon}"></i>
                    </div>
                    <div>
                        <div class="node-title">${node.name}</div>
                        <div class="node-subtitle">${node.location}</div>
                    </div>
                </div>
                <span class="node-state ${node.state}">${stateText}</span>
            </div>
        `;
    }).join('');
}

// Renderiza las Alertas Prioritarias 
function renderPriorityAlertsUI() {
    const priorityContainer = document.getElementById('priority-alerts-container');
    if (!priorityContainer) return;

    const priorityItems = alertsData.filter(a => {
        const norm = normalizeLevel(a.level);
        return (norm === 'high' || norm === 'critical') && a.status !== 'Atendido';
    });

    if (priorityItems.length === 0) {
        priorityContainer.innerHTML = `
            <div style="grid-column: 1 / -1; padding: 22px; text-align: center; color: var(--text-muted); background: var(--bg-subtle); border-radius: var(--radius-md);">
                <i class="fa-solid fa-circle-check" style="color: var(--accent-green); font-size: 1.6rem; margin-bottom: 8px; display: block;"></i>
                No hay alertas prioritarias activas en este momento.
            </div>
        `;
        return;
    }

    priorityContainer.innerHTML = priorityItems.map(item => {
        const norm = normalizeLevel(item.level);
        const isEval = item.metadata?.alert_evaluated !== false;
        const cleanTitle = String(item.title || '')
            .replace(/\[SIMULACI[ÓO]N\]\s*/gi, '')
            .replace(/>\s*3000\s*m/gi, '')
            .trim();

        return `
            <div class="priority-card ${norm}">
                <div class="priority-card-top">
                    ${renderRiskBadgeHTML(item.level, isEval)}
                    ${renderOriginBadgeHTML(item.origin_type)}
                </div>
                <h3>${cleanTitle}</h3>
                <div class="priority-card-meta">
                    <span><i class="fa-solid fa-location-dot"></i> ${item.source}</span>
                    <span><i class="fa-solid fa-clock"></i> ${item.time}</span>
                </div>
                <div>
                    <strong style="font-size: 0.72rem; text-transform: uppercase; color: var(--text-muted);">Canales de Difusión:</strong>
                    ${renderChannelsHTML(item.channels)}
                </div>
                <div class="priority-card-actions">
                    <span style="font-size: 0.76rem; font-weight: 600; color: var(--text-secondary);">
                        Estado: <strong>${item.status}</strong>
                    </span>
                    <button class="btn-action-sm" title="Confirmar / Atender alerta" onclick="toggleStatus(${item.id})">
                        <i class="fa-solid fa-check"></i>
                    </button>
                </div>
            </div>
        `;
    }).join('');
}

// Actualiza el contador de alertas activas
function updateKPIs() {
    const kpiActiveAlerts = document.getElementById('kpi-active-alerts');
    if (!kpiActiveAlerts) return;

    const activeCount = alertsData.filter(a => a.status !== 'Atendido').length;
    kpiActiveAlerts.textContent = activeCount;
}

// Renderiza la tabla completa
function renderHistoryTableUI() {
    const tableBody = document.getElementById('alerts-table-body');
    if (!tableBody) return;

    tableBody.innerHTML = '';
    alertsData.forEach((alert) => {
        const isEval = alert.metadata?.alert_evaluated !== false;
        const isoString = alert.isoDate || new Date().toISOString();
        const traceId = alert.metadata?.trace_id || `TRC-${String(alert.id).padStart(3, '0')}`;
        const cleanTitle = String(alert.title || '')
            .replace(/\[SIMULACI[ÓO]N\]\s*/gi, '')
            .replace(/>\s*3000\s*m/gi, '')
            .trim();

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>
                <span class="iso-timestamp">#${alert.id}</span>
                <span class="iso-sub">${traceId}</span>
            </td>
            <td>
                <span class="iso-timestamp">${isoString.replace('T', ' ').substring(0, 19)}</span>
                <span class="iso-sub">${alert.time}</span>
            </td>
            <td>
                <strong>${cleanTitle}</strong>
                ${renderChannelsHTML(alert.channels)}
            </td>
            <td>
                <div>${alert.source}</div>
                <div style="margin-top: 4px;">${renderOriginBadgeHTML(alert.origin_type)}</div>
            </td>
            <td>
                ${renderRiskBadgeHTML(alert.level, isEval)}
            </td>
            <td>
                <span style="font-weight: 600;">${alert.status}</span>
            </td>
            <td>
                <button class="btn-action-sm" title="Cambiar estado" onclick="toggleStatus(${alert.id})">
                    <i class="fa-solid fa-check"></i>
                </button>
            </td>
        `;
        tableBody.appendChild(tr);
    });
}

// Alternar estado de una alerta
window.toggleStatus = function (id) {
    alertsData = alertsData.map(alert => {
        if (alert.id === id) {
            if (alert.status === 'Pendiente') alert.status = 'Confirmado';
            else if (alert.status === 'Confirmado') alert.status = 'Atendido';
            else alert.status = 'Pendiente';
        }
        return alert;
    });

    localStorage.setItem('copahue_alerts_v2', JSON.stringify(alertsData));
    updateKPIs();
    renderPriorityAlertsUI();
    renderHistoryTableUI();
};

// Botón limpiar en historial.html
const btnClear = document.getElementById('btn-clear');
if (btnClear) {
    btnClear.addEventListener('click', () => {
        if (confirm('¿Deseas restablecer las alertas iniciales?')) {
            alertsData = [...ALERTAS_INICIALES];
            localStorage.setItem('copahue_alerts_v2', JSON.stringify(alertsData));
            updateKPIs();
            renderPriorityAlertsUI();
            renderHistoryTableUI();
        }
    });
}

// Inicialización del Mapa Satelital de Copahue
function initMapIfPresent() {
    const mapContainer = document.getElementById('copahue-map');
    if (!mapContainer || typeof L === 'undefined') return;

    const copahueCenter = [-37.855, -71.115];
    const map = L.map('copahue-map', {
        zoomControl: true,
        scrollWheelZoom: true
    }).setView(copahueCenter, 11);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap',
        maxZoom: 18
    }).addTo(map);

    const puntos = [
        { nombre: 'Cráter Principal Volcán Copahue', coords: [-37.859, -71.171], desc: 'Cráter activo El Agrio. Monitoreo de fumarolas y gases SO2.', estado: 'Alerta Amarilla' },
        { nombre: 'Termas de Copahue - Sirena Norte', coords: [-37.820, -71.095], desc: 'Sirena acústica de evacuación y baliza sector norte.', estado: 'Activo' },
        { nombre: 'Villa Caviahue - Centro Operativo', coords: [-37.880, -71.055], desc: 'Población urbana, centro de evacuación y red comunitaria.', estado: 'Normal' },
        { nombre: 'Estación Sísmica OVDAS / SEGEMAR', coords: [-37.865, -71.135], desc: 'Sismógrafo triaxial de banda ancha (flanco este del volcán).', estado: 'Operando' },
        { nombre: 'Repetidora Cerro Mellizo & Kit Solar', coords: [-37.842, -71.080], desc: 'Enlace de comunicaciones VHF/LoRa y panel fotovoltaico.', estado: 'Batería 98%' }
    ];

    puntos.forEach(p => {
        L.marker(p.coords).addTo(map)
            .bindPopup(`
                <div style="font-family: inherit;">
                    <strong style="color: #1C382B; font-size: 0.95rem;">${p.nombre}</strong><br>
                    <span style="color: #4A5B51; font-size: 0.85rem;">${p.desc}</span><br>
                    <div style="margin-top: 6px; font-size: 0.78rem; font-weight: 700; color: #15803D;">● ${p.estado}</div>
                </div>
            `);
    });
}
renderNodesUI();
renderPriorityAlertsUI();
updateKPIs();
renderHistoryTableUI();
initMapIfPresent();