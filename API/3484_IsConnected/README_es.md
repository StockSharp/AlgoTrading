# Estrategia Está Conectada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
* **Fuente**: Convertido del script MetaTrader 5 `IsConnected.mq5` (carpeta `MQL/35056`).
* **Propósito**: Supervisa continuamente el estado del conector e informa las transiciones en línea/fuera de línea con marcas de tiempo y duraciones de tiempo de actividad/inactividad.
* **Tipo**: Estrategia de servicios públicos centrada en el monitoreo de la infraestructura en lugar de la ejecución de órdenes.

## Comportamiento
1. Cuando se inicia la estrategia, registra inmediatamente que el módulo de monitoreo se ha inicializado y captura el estado actual del conector.
2. Un temporizador en segundo plano verifica la bandera `Connector.IsConnected` cada `CheckIntervalSeconds` (predeterminado: 1 segundo).
3. Cuando el estado cambia, la estrategia:
   * Almacena el momento de transición usando la estrategia `CurrentTime`.
   * Registra el nuevo estado (`Online` o `Offline`).
   * Informa cuánto duró el estado anterior (tiempo en línea antes de una desconexión o tiempo fuera de línea antes de la reconexión).
4. Cuando la estrategia se detiene, cancela el temporizador y registra el último estado conocido para que el operador sepa si la conexión estaba activa o inactiva en el momento del cierre.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
|------|------|---------|-------------|
| `CheckIntervalSeconds` | `int` | `1` | Intervalo (en segundos) entre comprobaciones de conexión sucesivas. Debe ser mayor que cero. |

## Detalles de registro
* Todos los mensajes están escritos con `LogInfo` en inglés para coincidir con la implementación de MetaTrader que se basó en declaraciones `Print`.
* Los intervalos de tiempo tienen formato utilizando una cultura invariante e incluyen marcas de tiempo de inicio y el tiempo transcurrido en el estado anterior.

## Diferencias vs guión original
* El bucle de espera ocupado de MQL5 se reemplaza con un temporizador administrado que no bloquea el subproceso de estrategia.
* En lugar de imprimir líneas de estado duplicadas, la versión StockSharp informa cambios de estado estructurados junto con métricas de tiempo de actividad/tiempo de inactividad.
* La conversión maneja la eliminación elegante deteniendo el temporizador tanto en `OnStopped` como en `OnReseted`.
