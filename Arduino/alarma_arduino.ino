#include <Wire.h>
#include <LiquidCrystal_I2C.h>

LiquidCrystal_I2C lcd(0x27, 16, 2);

// ---------------- Pines ----------------
const uint8_t yellow = 7;
const uint8_t red = 6;
const uint8_t green = 5;
const uint8_t blue = 4;

const uint8_t buzzLeft = 11;
const uint8_t buzzCentre = 12;
const uint8_t buzzRight = 13;
const uint8_t buzzExtra1 = 10;
const uint8_t buzzExtra2 = 9;

// ---------------- Tiempos ----------------
const unsigned long DURACION_ALARMA = 20000UL;     // 20 s por ciclo
const unsigned long INTERVALO_PANTALLA = 3000UL;   // cambio de pantalla en el LCD
const unsigned long INTERVALO_LED = 50;
const unsigned long INTERVALO_BUZZER = 25;

// ---------------- Pantallas del LCD ----------------
const uint8_t MAX_PANTALLAS = 8;
char pantallas[MAX_PANTALLAS][2][17];   // [pantalla][linea][16 chars + '\0']
uint8_t totalPantallas = 0;
uint8_t pantallaActual = 0;
unsigned long tiempoPantalla = 0;

// ---------------- Estado de la alarma ----------------
bool alarmaActiva = false;
bool nuevoCriticoEnCiclo = false;   // llego un dato critico NUEVO durante el ciclo actual
unsigned long inicioAlarma = 0;
unsigned long tiempoLed = 0;
unsigned long tiempoBuzzer = 0;
uint8_t pasoLed = 0;
uint8_t pasoBuzzer = 0;

// ---------------- Serial ----------------
const uint16_t TAM_BUFFER = 300;
char lineaSerial[TAM_BUFFER];
uint16_t idx = 0;
bool descartarLinea = false;

// ======================================================
//  Utilidades
// ======================================================
void apagarSalidas() {
    digitalWrite(yellow, LOW);
    digitalWrite(red, LOW);
    digitalWrite(green, LOW);
    digitalWrite(blue, LOW);
    digitalWrite(buzzLeft, LOW);
    digitalWrite(buzzCentre, LOW);
    digitalWrite(buzzRight, LOW);
    digitalWrite(buzzExtra1, LOW);
    digitalWrite(buzzExtra2, LOW);
}

void copiarLinea(char* destino, const char* origen) {
    strncpy(destino, origen, 16);
    destino[16] = '\0';
}

void dibujarPantalla() {
    if (totalPantallas == 0) return;
    lcd.clear();
    lcd.setCursor(0, 0);
    lcd.print(pantallas[pantallaActual][0]);
    lcd.setCursor(0, 1);
    lcd.print(pantallas[pantallaActual][1]);
}

void rotarPantallas(unsigned long ahora) {
    if (totalPantallas < 2) return;
    if (ahora - tiempoPantalla >= INTERVALO_PANTALLA) {
        tiempoPantalla = ahora;
        pantallaActual = (pantallaActual + 1) % totalPantallas;
        dibujarPantalla();
    }
}

// ======================================================
//  Alarma
// ======================================================
void iniciarAlarma() {
    alarmaActiva = true;
    nuevoCriticoEnCiclo = false;
    inicioAlarma = millis();
    pasoLed = 0;
    pasoBuzzer = 0;
    apagarSalidas();
    digitalWrite(buzzLeft, HIGH);   // pitido fijo de fondo
    Serial.println(F("EVT:ALARMA_INICIO"));
}

void detenerAlarma() {
    alarmaActiva = false;
    nuevoCriticoEnCiclo = false;
    apagarSalidas();
    Serial.println(F("EVT:ALARMA_FIN"));
}

void efectosLed(unsigned long ahora) {
    if (ahora - tiempoLed < INTERVALO_LED) return;
    tiempoLed = ahora;

    digitalWrite(yellow, LOW);
    digitalWrite(red, LOW);
    digitalWrite(green, LOW);
    digitalWrite(blue, LOW);

    switch (pasoLed) {
        case 0: digitalWrite(yellow, HIGH); digitalWrite(blue, HIGH); break;
        case 1: digitalWrite(red, HIGH); digitalWrite(green, HIGH); break;
        case 2: digitalWrite(blue, HIGH); digitalWrite(red, HIGH); break;
        case 3: digitalWrite(yellow, HIGH); digitalWrite(green, HIGH); digitalWrite(blue, HIGH); break;
        case 4: digitalWrite(yellow, HIGH); digitalWrite(red, HIGH); digitalWrite(green, HIGH); digitalWrite(blue, HIGH); break;
    }
    pasoLed = (pasoLed + 1) % 5;
}

void efectosBuzzer(unsigned long ahora) {
    if (ahora - tiempoBuzzer < INTERVALO_BUZZER) return;
    tiempoBuzzer = ahora;

    digitalWrite(buzzCentre, LOW);
    digitalWrite(buzzRight, LOW);
    digitalWrite(buzzExtra1, LOW);
    digitalWrite(buzzExtra2, LOW);

    switch (pasoBuzzer) {
        case 0: digitalWrite(buzzRight, HIGH); digitalWrite(buzzExtra1, HIGH); break;
        case 1: digitalWrite(buzzCentre, HIGH); digitalWrite(buzzExtra2, HIGH); break;
        case 2: digitalWrite(buzzRight, HIGH); digitalWrite(buzzCentre, HIGH); digitalWrite(buzzExtra1, HIGH); break;
        case 3: digitalWrite(buzzExtra2, HIGH); break;
        case 4: digitalWrite(buzzCentre, HIGH); digitalWrite(buzzExtra1, HIGH); digitalWrite(buzzExtra2, HIGH); break;
        case 5: digitalWrite(buzzRight, HIGH); digitalWrite(buzzExtra2, HIGH); break;
    }
    pasoBuzzer = (pasoBuzzer + 1) % 6;
}

// Cada 20 s decide: si llego otro critico nuevo -> otro ciclo; si no -> se corta.
void controlarCicloAlarma(unsigned long ahora) {
    if (!alarmaActiva) return;
    if (ahora - inicioAlarma < DURACION_ALARMA) return;

    if (nuevoCriticoEnCiclo) {
        nuevoCriticoEnCiclo = false;
        inicioAlarma = ahora;
        Serial.println(F("EVT:ALARMA_RENUEVA"));
    } else {
        detenerAlarma();
    }
}

// ======================================================
//  Protocolo serial
//  Formato:  estado|nuevo|l1|l2|l1|l2|...\n
//    estado: 0 = no critico, 1 = critico, 2 = solo mostrar texto (no toca la alarma)
//    nuevo : 1 = es un registro critico que todavia no se habia informado
//    luego pares de lineas (max 16 caracteres c/u), una pareja por pantalla
// ======================================================
char* siguienteCampo(char** cursor) {
    if (*cursor == NULL) return NULL;
    char* inicio = *cursor;
    char* sep = strchr(inicio, '|');
    if (sep) {
        *sep = '\0';
        *cursor = sep + 1;
    } else {
        *cursor = NULL;
    }
    return inicio;
}

void procesarLinea(char* msg) {
    char* cursor = msg;
    char* campoEstado = siguienteCampo(&cursor);
    char* campoNuevo = siguienteCampo(&cursor);
    if (!campoEstado || !campoNuevo) return;   // mensaje invalido, se ignora

    int estado = atoi(campoEstado);
    bool nuevo = (atoi(campoNuevo) == 1);
    if (estado < 0 || estado > 2) return;

    uint8_t n = 0;
    while (n < MAX_PANTALLAS) {
        char* l1 = siguienteCampo(&cursor);
        if (!l1) break;
        char* l2 = siguienteCampo(&cursor);
        copiarLinea(pantallas[n][0], l1);
        copiarLinea(pantallas[n][1], l2 ? l2 : "");
        n++;
    }
    if (n == 0) return;

    totalPantallas = n;
    if (nuevo || pantallaActual >= totalPantallas) pantallaActual = 0;
    tiempoPantalla = millis();
    dibujarPantalla();

    if (estado == 1) {
        if (nuevo) {
            if (!alarmaActiva) iniciarAlarma();
            else nuevoCriticoEnCiclo = true;
        }
    } else if (estado == 0) {
        if (alarmaActiva) detenerAlarma();   // llego un dato no critico: se corta
    }
    // estado 2: no se toca la alarma
}

void leerSerial() {
    while (Serial.available()) {
        char c = Serial.read();
        if (c == '\r') continue;
        if (c == '\n') {
            if (!descartarLinea && idx > 0) {
                lineaSerial[idx] = '\0';
                procesarLinea(lineaSerial);
            }
            idx = 0;
            descartarLinea = false;
        } else if (idx < TAM_BUFFER - 1) {
            lineaSerial[idx++] = c;
        } else {
            descartarLinea = true;   // linea demasiado larga
        }
    }
}

// ======================================================
void setup() {
    Serial.begin(9600);
    lcd.init();
    lcd.backlight();

    pinMode(yellow, OUTPUT);
    pinMode(red, OUTPUT);
    pinMode(green, OUTPUT);
    pinMode(blue, OUTPUT);

    pinMode(buzzLeft, OUTPUT);
    pinMode(buzzCentre, OUTPUT);
    pinMode(buzzRight, OUTPUT);
    pinMode(buzzExtra1, OUTPUT);
    pinMode(buzzExtra2, OUTPUT);

    apagarSalidas();

    copiarLinea(pantallas[0][0], "Esperando datos");
    copiarLinea(pantallas[0][1], "");
    totalPantallas = 1;
    dibujarPantalla();
}

void loop() {
    unsigned long ahora = millis();

    leerSerial();
    rotarPantallas(ahora);
    controlarCicloAlarma(ahora);

    if (alarmaActiva) {
        efectosLed(ahora);
        efectosBuzzer(ahora);
    }
}
