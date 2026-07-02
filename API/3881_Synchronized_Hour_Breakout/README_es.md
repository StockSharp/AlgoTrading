# Estrategia de ruptura de horas sincronizadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de ruptura de horas sincronizadas** es una versión StockSharp del MetaTrader 4 asesor experto `JK_sinkhro1`. Analiza el saldo de velas alcistas y bajistas durante la ventana de negociación reciente y solo opera durante dos horas de sincronización cuidadosamente seleccionadas (por defecto, 19:00 y 22:00 más un desplazamiento). La estrategia se centra en capturar rupturas direccionales y al mismo tiempo hacer cumplir reglas conservadoras de gestión de riesgos similares a las del EA original.

## Lógica de trading
- Funciona en la serie de velas seleccionada por el parámetro `Candle Type` (predeterminado: velas de 1 hora).
- Mantiene una ventana deslizante de las últimas `Analysis Period` velas completadas y cuenta cuántas cerraron alcistas y bajistas.
- Cuando el conteo alcista excede el conteo bajista, la estrategia se prepara para una ruptura larga durante la primera hora de sincronización (`22 + Hour Offset`).
- Cuando el conteo bajista excede el conteo alcista, se prepara para una breve ruptura durante la segunda hora de sincronización (`19 + Hour Offset`).
- Las señales solo son válidas dentro de los primeros cinco minutos de la hora para que la orden se sincronice con la nueva barra abierta, como en el MQL original.
- Las nuevas operaciones se ignoran si ya hay `Max Active Orders` registrado o si hay una posición abierta.

## Gestión de Riesgos y Gestión Comercial
- Las posiciones se abren con un tamaño de lote fijo (`Fixed Volume`) o un tamaño basado en el riesgo utilizando el parámetro efectivo de la cuenta y `Risk %`. El modelo de riesgo divide el riesgo de efectivo permitido por la distancia de parada en pasos de precio para aproximarse al comportamiento de la fuente EA.
- Cada posición utiliza tres capas de lógica de salida:
  - Una toma de ganancias primaria a `Take Profit (pts)` del precio de entrada.
  - Una toma de ganancias secundaria más rápida en `Secondary TP (pts)` para imitar el cierre manual inicial en el código original.
  - Un stop-loss estricto en `Stop Loss (pts)` por debajo/por encima del precio de entrada.
- Trailing stop opcional: una vez que el precio avanza más de `Trailing Stop (pts)`, el umbral de seguimiento sigue el extremo favorable y cierra la posición si el precio retrocede más allá de la distancia de seguimiento.
- El estado de posición se restablece después de cada salida completa para prepararse para la siguiente ventana de sincronización.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `Take Profit (pts)` | Distancia de obtención de beneficios primaria en los escalones del precio de los valores. |
| `Secondary TP (pts)` | Distancia de toma de ganancias más rápida activada antes del objetivo principal. |
| `Stop Loss (pts)` | Distancia de stop-loss medida en pasos de precio. |
| `Trailing Stop (pts)` | Distancia del trailing stop; configúrelo en 0 para desactivarlo. |
| `Analysis Period` | Número de velas recientes inspeccionadas al contar los cierres alcistas/bajistas. |
| `Hour Offset` | Compensación agregada al horario comercial original de 19:00 y 22:00. |
| `Max Active Orders` | Número máximo de órdenes activas simultáneamente permitidas antes de que se bloqueen nuevas entradas. |
| `Fixed Volume` | Volumen comercial utilizado cuando el tamaño basado en riesgo está deshabilitado. |
| `Use Risk Volume` | Permite dimensionar posiciones dinámicamente en función del efectivo de la cartera y la distancia de parada. |
| `Risk %` | Porcentaje de efectivo de la cartera arriesgado por operación en modo basado en riesgo. |
| `Candle Type` | Tipo de vela/período de tiempo utilizado para los cálculos y la generación de señales. |

## Notas de uso
- La configuración predeterminada emula la versión MetaTrader que negoció EURUSD durante la sesión de Nueva York; ajuste el desplazamiento horario para que coincida con la zona horaria de su agente/servidor.
- Asegúrese de que la definición de seguridad proporcione valores `PriceStep`, `VolumeStep` y `MinVolume` precisos para que el tamaño de la posición basado en el riesgo pueda alinear los volúmenes con los incrementos de los lotes de intercambio.
- Debido a que la estrategia se basa en datos de cierre de velas, conéctela a un proveedor de historial o a una fuente de datos en vivo que pueda entregar la serie de velas seleccionada con un retraso mínimo.
- La salida final utiliza precios de cierre de velas terminadas, que coinciden estrechamente con la lógica de seguimiento basada en ticks de la fuente EA sin dejar de ser compatible con el nivel alto de StockSharp API.
