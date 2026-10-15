# Estrategia Canal EA con Órdenes Límite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- **Origen**: convertida del expert de MetaTrader 5 `ChannelEA1.mq5`.
- **Propósito**: monitorear un canal de precio intradía entre dos horas definidas por el usuario y colocar órdenes límite al final de esa ventana.
- **Enfoque**: la estrategia realiza un seguimiento de los precios más altos y más bajos observados durante la sesión y coloca órdenes límite simétricas para operar posibles reversiones de vuelta al lado opuesto del canal.

La estrategia es adecuada para instrumentos que exhiben reversión a la media una vez que se establece un rango diario. Por diseño funciona en cuentas de compensación: una orden de venta límite ejecutada cerrará una posición larga existente antes de abrir una nueva corta y viceversa.

## Parámetros
| Nombre | Por defecto | Descripción |
| --- | --- | --- |
| `BeginHour` | `1` | Hora (0-23) cuando comienza el seguimiento del rango intradía. La estrategia cancela órdenes pendientes y cierra posiciones en este momento. |
| `EndHour` | `10` | Hora (0-23) cuando se evalúa el rango acumulado y se colocan nuevas órdenes límite. Soporta sesiones nocturnas: si `BeginHour > EndHour`, la sesión abarca la medianoche. |
| `OrderVolume` | `1` | Volumen aplicado a cada orden pendiente. |
| `CandleType` | Marco temporal de `1 hora` | Serie de velas utilizada para construir el canal. Puede cambiar a cualquier marco temporal soportado por StockSharp. |

## Lógica de trading
1. **Manejo de sesión**
   - La estrategia deriva los timestamps de inicio y fin de sesión de los parámetros `BeginHour` y `EndHour` usando los timestamps de las velas. Cuando `BeginHour > EndHour`, el fin se traslada al día siguiente.
   - En la primera vela terminada cuyo tiempo de cierre alcanza el límite de inicio, la estrategia cancela todas las órdenes activas, cierra la posición abierta y restablece las estadísticas de sesión.
2. **Construcción del canal**
   - Solo las velas cuyo tiempo de apertura se encuentra dentro de la ventana de sesión contribuyen al rango. La estrategia mantiene el máximo y mínimo corrientes para la sesión y cuenta el número de velas contribuyentes.
   - Se requieren al menos dos velas terminadas para formar un rango válido, replicando el comportamiento del expert MQL5 original (condición `n > 2`).
3. **Colocación de órdenes al final de sesión**
   - Cuando una vela terminada cruza el límite de fin, la estrategia verifica que el rango se ha formado y que el mínimo está estrictamente por debajo del máximo.
   - Luego coloca dos órdenes pendientes:
     - `BuyLimit` en el mínimo de sesión registrado con volumen `OrderVolume`.
     - `SellLimit` en el máximo de sesión registrado con el mismo volumen.
   - Las órdenes permanecen activas hasta que comienza la próxima sesión. Dado que la estrategia corre en cuenta de compensación, estas órdenes sirven tanto como entradas como salidas: por ejemplo, el `SellLimit` cierra una posición larga existente en el máximo de sesión antes de establecer una nueva corta.
4. **Preparación de la próxima sesión**
   - En el próximo límite de inicio, la estrategia cierra cualquier posición restante y elimina las órdenes pendientes sobrantes antes de medir el nuevo canal.

## Notas adicionales
- No se establece stop-loss explícito. La gestión de riesgos debe controlarse a través del dimensionamiento de posición, anulaciones manuales o lógica protectora externa.
- La lógica usa solo velas terminadas (`CandleStates.Finished`) para mantenerse alineada con el comportamiento del EA original.
- Asegúrese de que la zona horaria del feed de datos y del servidor coincida con sus expectativas, porque los límites de sesión se evalúan en tiempo de bolsa/local.
- Al optimizar, considere tanto las horas de trading como la duración de la vela; la estrategia es sensible a la combinación porque el rango registrado depende del marco temporal seleccionado.
