# Estrategia del sistema Otkat
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia reproduce el asesor experto MetaTrader **1_Otkat_Sys**. Supervisa la apertura, el cierre, el máximo y el máximo del día de negociación anterior.
y bajo para decidir si ingresar una posición durante los primeros tres minutos después de la medianoche (hora del corredor) del martes al
Jueves.

## Lógica de trading

1. **Estadísticas diarias**: la última vela diaria completada se almacena en caché para calcular:
   - `Open - Close` y `Close - Open` para detectar si la sesión anterior fue bajista o alcista.
   - `Close - Low` y `High - Close` para medir en qué medida el precio retrocedió desde los extremos.
2. **Ventana de entrada**: las nuevas operaciones se evalúan cuando la vela de entrada se abre entre las 00:00 y las 00:03. lunes y viernes son
omitido, coincidiendo con los filtros `DayOfWeek` del robot original.
3. **Filtros direccionales**: cuatro condiciones mutuamente excluyentes reflejan las reglas MQL:
   - Día anterior bajista (`Open - Close` por encima del umbral del corredor) combinado con un retroceso superficial (`Close - Low`
debajo de `Pullback - Tolerance`) abre un largo.
   - El día anterior alcista con un retroceso alcista extendido (`High - Close` por encima de `Pullback + Tolerance`) también abre una posición larga.
   - El día anterior alcista con un retroceso alcista débil (`High - Close` por debajo de `Pullback - Tolerance`) abre una venta corta.
   - El día anterior bajista con un retroceso bajista extendido (`Close - Low` por encima de `Pullback + Tolerance`) abre una posición corta.
4. **Órdenes**: las entradas son órdenes de mercado realizadas con el tamaño de lote configurado. Las operaciones de compra utilizan una distancia de obtención de beneficios igual a
`TakeProfit + 3` puntos (como en el EA original); Los pantalones cortos usan exactamente `TakeProfit` puntos. Ambas partes aplican el mismo stop-loss
distancia.
5. **Salida basada en tiempo**: cualquier posición abierta se aplana después de las 22:45, replicando la limpieza nocturna implementada en el MetaTrader
guión.

Todos los parámetros de umbral se expresan en puntos y se traducen a distancias de precios con el `PriceStep` del instrumento.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `EntryCandleType` | Marco de tiempo utilizado para la ventana de negociación (predeterminado: 1 minuto). |
| `DailyCandleType` | Plazo que proporciona las estadísticas diarias (predeterminado: 1 día). |
| `TakeProfit` | Objetivo de beneficio en puntos. Las operaciones largas añaden un colchón de 3 puntos. |
| `StopLoss` | Distancia de parada de protección en puntos. |
| `PullbackThreshold` | Umbral de retroceso base ("Otkat") en puntos. |
| `CorridorThreshold` | Umbral del corredor direccional (`KoridorOC`). |
| `ToleranceThreshold` | Tolerancia de retroceso (`KoridorOt`). |
| `TradeVolume` | Tamaño del lote para cada entrada. |

La estrategia restablece automáticamente sus valores almacenados en caché el `Reset`, se suscribe a flujos de velas tanto de entrada como diarios, y extrae
velas más marcadores comerciales cuando un área del gráfico esté disponible.
