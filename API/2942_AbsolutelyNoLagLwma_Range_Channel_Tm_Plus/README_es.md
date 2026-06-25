# Estrategia AbsolutelyNoLagLWMA Canal de Rango TM Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port directo del experto MetaTrader "Exp_AbsolutelyNoLagLwma_Range_Channel_Tm_Plus". Opera un canal de precios derivado de una media móvil ponderada linealmente (LWMA) de doble suavizado de los máximos y mínimos de las velas. La versión StockSharp mantiene el comportamiento original: las señales se evalúan en velas finalizadas de un marco temporal seleccionable, el estado del canal se codifica de la misma manera que el indicador MQL, y la gestión de posiciones sigue el mismo orden de prioridad (salida por tiempo primero, salidas por indicador segundo, nuevas entradas al final).

## Construcción del indicador
1. Para cada vela finalizada, las series de máximos y mínimos se introducen en una primera LWMA. El parámetro de longitud se comparte entre los flujos de máximos y mínimos.
2. La salida de la primera LWMA se suaviza nuevamente con otra LWMA de la misma longitud. Esto recrea el suavizado "AbsolutelyNoLagLWMA" utilizado por el indicador original.
3. Los valores finales del canal superior e inferior se comparan con el cierre de la vela:
   * Cierre por encima de la línea superior → estado de ruptura alcista.
   * Cierre por debajo de la línea inferior → estado de ruptura bajista.
   * Cierre dentro del canal → estado neutral.
4. La estrategia almacena los estados de canal más recientes. El parámetro `SignalBar` controla qué índice de barra se comprueba para la generación de señales (0 = última vela cerrada, 1 = una barra atrás, etc.), coincidiendo con la entrada `SignalBar` del programa MQL.

## Interpretación de señales
* **Entrada larga** – habilitada por `EnableBuyEntries`. La estrategia busca una ruptura alcista en la barra indexada por `SignalBar + 1` mientras la barra en `SignalBar` ya ha retornado dentro del canal. El comportamiento replica la prueba original de "ruptura en barra anterior".
* **Entrada corta** – habilitada por `EnableSellEntries`. Refleja la lógica larga para rupturas bajistas.
* **Salida larga** – habilitada por `EnableBuyExits`. Una ruptura bajista en la barra de referencia cierra posiciones largas existentes, a menos que ya hayan sido cerradas por la salida basada en tiempo en la vela actual.
* **Salida corta** – habilitada por `EnableSellExits`. Una ruptura alcista en la barra de referencia cierra posiciones cortas abiertas, a menos que la salida basada en tiempo ya haya solicitado el cierre.

## Gestión de operaciones
* **Volumen de orden** – tomado del parámetro `OrderVolume`. Las órdenes de reversión añaden automáticamente el valor absoluto de la posición actual para evitar exposición residual.
* **Stop loss / Take profit** – compensaciones absolutas opcionales definidas en puntos del instrumento (`StopLossPoints`, `TakeProfitPoints`). Cuando son positivos se convierten a compensaciones de precio usando el `PriceStep` del instrumento y se pasan a `StartProtection`.
* **Salida basada en tiempo** – el EA original cierra posiciones que exceden un tiempo de mantenimiento (`TimeTrade`, `nTime`). En StockSharp esto se maneja mediante `UseTimeExit` y `HoldingLimit`. La salida se evalúa antes que las señales del indicador en cada vela finalizada.
* **Temporización de posición** – la estrategia registra la marca de tiempo de la última operación que resultó en una posición larga o corta. Estas marcas de tiempo se usan para la salida basada en tiempo.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `Length` | Longitud de ambas pasadas LWMA que forman el canal. |
| `SignalBar` | Desplazamiento de la barra examinada para señales (0 = última vela cerrada). |
| `CandleType` | Marco temporal utilizado para el indicador y la evaluación de operaciones. |
| `OrderVolume` | Volumen utilizado al enviar nuevas órdenes de entrada. |
| `StopLossPoints` | Distancia de stop-loss en puntos del instrumento (0 deshabilita el stop). |
| `TakeProfitPoints` | Distancia de take-profit en puntos del instrumento (0 deshabilita el objetivo). |
| `EnableBuyEntries` | Permitir o prohibir nuevas posiciones largas. |
| `EnableSellEntries` | Permitir o prohibir nuevas posiciones cortas. |
| `EnableBuyExits` | Permitir que el indicador cierre posiciones largas. |
| `EnableSellExits` | Permitir que el indicador cierre posiciones cortas. |
| `UseTimeExit` | Habilitar el cierre de posiciones después de que transcurra `HoldingLimit`. |
| `HoldingLimit` | Tiempo máximo de mantenimiento antes de que se active la salida por tiempo. |

## Notas
* El canal se calcula a partir de los máximos y mínimos de las velas exactamente como el indicador MQL incluido `AbsolutelyNoLagLwma_Range_Channel`.
* La estrategia ignora las velas incompletas y trabaja solo con datos completados para evitar señales prematuras.
* Establecer `SignalBar` en `0` coincide con la configuración típica de MT5 donde se analiza la última vela cerrada. Valores más altos reproducen la confirmación retardada utilizada por el EA predeterminado (`SignalBar = 1`).
* Si `PriceStep` no está disponible para el instrumento seleccionado, los desplazamientos de stop-loss y take-profit se ignoran, preservando el comportamiento de las entradas con valor cero en el script original.
