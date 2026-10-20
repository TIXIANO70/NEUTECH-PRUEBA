# NEUTECH (Sistema de comunicaciones y alertas de emergencia)

Sistema distribuido de comunicación y gestión de alertas de emergencia,
pensado para comunidades cordilleranas expuestas a riesgos ambientales y
a interrupciones de los servicios esenciales.

> **Proyecto:** Navathon III --- Grupo 3, Tema 5\
> **Objetivo:** Mejorar la recepción, procesamiento y difusión de
> alertas, incluso en condiciones geográficas, climáticas o de
> conectividad adversas.

## Índice
-   [Descripción general](#descripción-general)
-   [Problema que aborda](#problema-que-aborda)
-   [Objetivos y alcance](#objetivos-y-alcance)
-   [Características principales](#características-principales)
-   [Arquitectura del sistema](#arquitectura-del-sistema)
-   [Tecnologías utilizadas](#tecnologías-utilizadas)
-   [Componentes electrónicos](#componentes-electrónicos)
-   [Dashboard web](#dashboard-web)
-   [Backend y API](#backend-y-api)
-   [Base de datos y persistencia](#base-de-datos-y-persistencia)
-   [Organización del equipo](#organización-del-equipo)
-   [Limitaciones y trabajo futuro](#limitaciones-y-trabajo-futuro)

## Descripción general

El **Sistema de Comunicaciones y Alertas de Emergencia (SCAE)** integra
dispositivos físicos, un servidor, una base de datos y una aplicación
web para recibir y procesar eventos de riesgo, emitir alertas locales
visuales y sonoras, registrar confirmaciones y facilitar el seguimiento
de las emergencias desde un centro de monitoreo.

El proyecto está orientado a la región cordillerana de
**Copahue-Caviahue**, donde las condiciones geográficas y climáticas
pueden dificultar el acceso y las comunicaciones.

## Problema que aborda
La región puede estar expuesta a situaciones como:
-   Erupciones volcánicas.
-   Incendios forestales.
-   Nevadas intensas.
-   Cortes prolongados de servicios esenciales.
-   Bloqueo de caminos y fallas de los canales tradicionales de
    comunicación.
Estas condiciones pueden retrasar la transmisión de información crítica
y aumentar la vulnerabilidad de las comunidades. El SCAE busca ofrecer
una alternativa de alerta y monitoreo coordinado.

## Objetivos y alcance
### Objetivos
-   Recibir y procesar eventos de riesgo.
-   Activar alertas físicas locales mediante señales visuales y sonoras.
-   Enviar y registrar confirmaciones de estado y recepción.
-   Supervisar alertas, conectividad y estado de los nodos desde un
    dashboard web.
-   Conservar un historial de eventos para apoyar el seguimiento y la
    toma de decisiones.
-   Facilitar la comunicación bidireccional con otros grupos operativos
    mediante una API.
-   Favorecer la continuidad operativa con alimentación energética
    autónoma.

### Alcance
El sistema contempla:
-   Nodos o subestaciones físicas basadas en microcontroladores.
-   Un dashboard web centralizado para operadores.
-   Un backend y una API de integración.
-   Persistencia de alertas, niveles de criticidad y confirmaciones.
-   Integración con otros grupos operativos, incluido el Grupo 6.
-   Uso previsto de kits solares fotovoltaicos o sistemas eólicos con
    baterías para mejorar la autonomía energética.

**Fuera del alcance declarado:** transacciones comerciales y cobros,
servicios complejos de telemedicina y rastreo satelital de vehículos
particulares.

## Características principales
-   **Alertas físicas:** señales luminosas y sonoras en el sitio.
-   **Monitoreo centralizado:** visualización de eventos y estado de las
    subestaciones.
-   **Registro histórico:** conservación estructurada de alertas y
    confirmaciones.
-   **Interoperabilidad:** intercambio de información con sistemas de
    otros grupos.
-   **Arquitectura distribuida:** nodos desplegados en territorio para
    reducir la dependencia de un único punto.
-   **Diseño web adaptable:** interfaz preparada para pantallas de
    escritorio y dispositivos móviles.
-   **Autonomía energética prevista:** alimentación mediante fuentes
    renovables y baterías.

## Arquitectura del sistema
El flujo general descrito en la documentación es el siguiente:
1.  Las fuentes externas o los grupos operativos generan o transmiten
    información sobre eventos de riesgo.
2.  La información llega a través de la API y es recibida por el
    sistema.
3.  Dispositivo ESP32 se encarga de levantar una red local inalambrica IEEE 802.11
4.  El servidor procesa y estructura la información.
5.  Los eventos se guardan en PostgreSQL para conservar el historial.
6.  El dashboard web consulta la API y presenta las alertas y el estado
    de los nodos.
7.  El sistema envía órdenes a la etapa física de salida, donde se
    utilizan componentes como la pantalla LCD, la matriz LED, los LEDs y
    el buzzer para comunicar la alerta.
    
## Tecnologías utilizadas

  Área                                y sus tecnologías documentadas

  Firmware y hardware:                 NodeMCU ESP-32S, Arduino IDE, C/C++

  Backend:                             C#, .NET 8, ASP.NET Core Minimal
                                      API

  Comunicación del servidor:           `HttpClient`, `BackgroundService`,
                                      mensajes JSON con
                                      `System.Text.Json`

  Frontend:                            HTML, CSS, JavaScript y `fetch`

  Base de datos:                       PostgreSQL y SQL

  Alojamiento de base de datos:        Neon

## Componentes electrónicos

Los siguientes componentes aparecen en la documentación técnica:

  Componente                          y su función prevista

  NodeMCU ESP-32S v1.1 (CP2102        Microcontrolador principal, con
  USB-UART):                           Wi-Fi y Bluetooth integrados

  Sensor de sonido KY-037:             Captura señales de sonido mediante
                                      salidas analógica y digital

  Matriz LED 8×8:                      Representación visual de patrones o
                                      estados de alerta

  Módulo de relé de un canal (5 V /   Conmutación controlada de un
  10 A):                               circuito

  Pantalla LCD 16×2 con interfaz      Visualización de mensajes
  I2C/IIC:                             

  LEDs indicadores:                    Señalización visual

  Resistencias de 100 Ω:               Limitación de corriente y
                                      protección en circuitos LED, según
                                      el documento

  Buzzer:                              Emisión de alertas sonoras

  Kit solar fotovoltaico:              Alimentación autónoma prevista, con
                                      panel, regulador y batería

> **Imagen recomendada:** una foto real del prototipo armado, guardada
> como `docs/prototipo-fisico.jpg`. Colocarla debajo de esta sección o
> al inicio de la sección de hardware.

```{=html}
<!-- IMAGEN: docs/prototipo-fisico.jpg -->
```
```{=html}
<!-- Cuando el archivo exista, descomentar la siguiente línea: -->
```
```{=html}
<!-- ![Prototipo físico del SCAE](docs/prototipo-fisico.jpg) -->
```
También se puede agregar un esquema de conexiones como
`docs/conexiones-electronicas.png`. Conviene que la imagen corresponda
al circuito real implementado.

```{=html}
<!-- IMAGEN: docs/conexiones-electronicas.png -->
```
```{=html}
<!-- Cuando el archivo exista, descomentar la siguiente línea: -->
```
```{=html}
<!-- ![Esquema de conexiones electrónicas](docs/conexiones-electronicas.png) -->
```
## Dashboard web
El dashboard es el centro de monitoreo para los operadores. Según el
documento, contempla:
-   Mapa interactivo de la región con ubicaciones y estados de los
    nodos.
-   Indicadores generales de alertas activas y estado del sistema de
    energía.
-   Panel de alertas prioritarias con origen, fecha y hora, nivel de
    gravedad y canales de difusión.
-   Estado individual de las subestaciones: activas, desconectadas o con
    fallas.
-   Registro cronológico de eventos y alertas.
-   Controles para la emisión manual de avisos.
-   Actualización de información mediante solicitudes asíncronas a la
    API.
<img width="1365" height="767" alt="Vistazo" src="https://github.com/user-attachments/assets/315cf1ba-44e7-4ce7-9bf3-3766dda86d1f" />
<img width="1365" height="767" alt="Dashboard2" src="https://github.com/user-attachments/assets/8a8516b6-6b45-4c5d-88e3-0264f4759768" />
<img width="1365" height="767" alt="Historial" src="https://github.com/user-attachments/assets/6a99159c-2576-4720-8e0b-ff03143ae558" />

## Backend y API
El backend está documentado con **C# / .NET 8 y ASP.NET Core Minimal
API**. Su responsabilidad es centralizar la lógica del servidor y
comunicar el dashboard, los nodos físicos y los sistemas externos.

La documentación menciona endpoints como `/events`, `/records` y
`/alerts`, además de operaciones para recibir eventos, emitir
confirmaciones y consultar el estado general.

Otros elementos descritos:
-   `HttpClient` y `BackgroundService` para consultar las API de otros
    grupos periódicamente, con un intervalo documentado de 10 segundos.
-   `System.Text.Json` para leer y generar mensajes JSON.
-   `API/program.cs` como punto principal de configuración del servidor
    y definición de endpoints.
-   `API/dominio/Evento.cs` como modelo de evento, con datos como
    identificador, tipo de contingencia, criticidad, mensaje, fecha/hora
    de registro y nodo de origen.

## Base de datos y persistencia

El proyecto utiliza **PostgreSQL**, con la base de datos alojada en
**Neon**, para conservar información de forma estructurada.

El almacenamiento busca mantener registros de:
-   Eventos y alertas de emergencia.
-   Niveles de criticidad.
-   Estados de subestaciones.
-   Confirmaciones de recepción.
-   Historial cronológico para trazabilidad y análisis operativo.

<img width="1236" height="654" alt="1000114822" src="https://github.com/user-attachments/assets/4175edda-89ea-475e-b64e-b97e18a40d2e" />

## Organización del equipo
La documentación distribuye las responsabilidades de la siguiente
manera:
  Área                                Integrantes y responsabilidades
  Hardware / Firmware                 Valentino Masetto e Ignacio
                                      Gonzales: ensamblaje del circuito
                                      ESP32, programación con Arduino IDE
                                      y gestión de energía solar

  Backend / API                       Sasha Busto y Johanna Marca:
                                      endpoints, conexión con la base SQL
                                      y comunicación

  Base de datos                       Mateo Retamozo, Salvador Emiliano
                                      Garcia Delfabro y Alan Landesman:
                                      diagramas, entidades, campos y
                                      PostgreSQL en Neon

  Frontend web                        Zamira Valentin: diseño del
                                      dashboard adaptable en HTML/CSS y
                                      pruebas de integración general

  Documentación                       Araceli Camacho y Emanuel Bello:
                                      documentación técnica y bitácora

  Tablero Kanban                      Araceli Camacho: gestión de tareas,
                                      problemas, soluciones y avances en
                                      Trello

## Limitaciones y trabajo futuro
### Limitaciones identificadas en el alcance
-   El sistema se centra en la comunicación de alertas físicas y
    digitales, la recepción de eventos y la conectividad resiliente.
-   No incluye transacciones comerciales, cobros, telemedicina compleja
    ni seguimiento satelital de vehículos particulares.

### Mejoras previstas
Como objetivo de corto plazo, se plantea validar e implementar los
prototipos físicos y el dashboard interconectado en una primera prueba
piloto regional. A largo plazo, se propone consolidar el Sistema de comunicación y alertas como
infraestructura tecnológica de emergencia para comunidades cordilleranas
aisladas e incorporar en futuras versiones más capacidades de
sensorización ambiental avanzada.

### Pruebas documentadas


------------------------------------------------------------------------

**NEUTECH --- Sistema de Comunicaciones y Alertas de Emergencia**\
*Proyecto desarrollado para Navathon III.*
