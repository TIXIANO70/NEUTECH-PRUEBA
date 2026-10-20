import os
import time
import psycopg2
import serial

# Configuración desde variables de entorno
DATABASE_URL = os.environ.get("DATABASE_URL")
ARDUINO_PORT = os.environ.get("ARDUINO_PORT", "COM9")
BAUD_RATE = 9600

def main():
    if not DATABASE_URL:
        print("Error: La variable de entorno DATABASE_URL no está configurada.")
        return

    print(f"Intentando conectar al Arduino en {ARDUINO_PORT}...")
    try:
        ser = serial.Serial(ARDUINO_PORT, BAUD_RATE, timeout=1)
        time.sleep(2)  # Esperar reinicio del Arduino
        print(f"Conectado al puerto {ARDUINO_PORT}. Consultando Neon cada 3 s...")
    except Exception as e:
        print(f"Error al abrir puerto serie {ARDUINO_PORT}: {e}")
        return

    while True:
        try:
            conn = psycopg2.connect(DATABASE_URL)
            cur = conn.cursor()

            # Consulta simplificada sin dependencias complejas en confirmation
            query = """
                SELECT 
                    a.alert_id, 
                    a.alert_status, 
                    a.message, 
                    a.channels, 
                    r.record_id, 
                    r.record_type, 
                    r.timestamp, 
                    r.status, 
                    r.alert_level, 
                    n.node_id
                FROM alerts a
                JOIN record r ON a.record_id = r.record_id
                LEFT JOIN node n ON r.node_id = n.node_id
                WHERE a.alert_status = 'active'
                ORDER BY 
                    CASE WHEN r.alert_level = 'critical' THEN 1 ELSE 2 END,
                    r.timestamp DESC
                LIMIT 1;
            """
            
            cur.execute(query)
            row = cur.fetchone()
            
            if row:
                alert_id, alert_status, message, channels, record_id, record_type, r_time, r_status, alert_level, node_id = row
                
                # Paquete de datos por Serial hacia el Arduino
                payload = f"ID:{alert_id}|LVL:{alert_level}|MSG:{message}|NODE:{node_id}|STAT:{alert_status}\n"
                ser.write(payload.encode('utf-8'))
            else:
                # Sin alertas activas
                ser.write(b"STATUS:OK|MSG:SIN ALERTAS ACTIVAS\n")

            cur.close()
            conn.close()

        except psycopg2.Error as db_err:
            print(f"Error de base de datos: {db_err}")
            try:
                ser.write(b"STATUS:ERROR|MSG:BD SIN CONEXION\n")
            except:
                pass
        except Exception as ex:
            print(f"Error general: {ex}")

        time.sleep(3)

if __name__ == "__main__":
    main()