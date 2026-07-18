# FT CCI MA (Port de StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Esta estrategia es un port directo del asesor experto de MetaTrader "FT CCI MA". Opera al cierre de cada vela terminada, combinando una media móvil ponderada lineal (LWMA) con umbrales del Commodity Channel Index (CCI) y un filtro de sesión de trading opcional. La implementación de StockSharp mantiene los mismos nombres de parámetros y valores por defecto, permitiéndote reproducir el comportamiento original mientras te beneficias de la API de alto nivel (suscripciones a velas, vinculación de indicadores, protección de posición).

Notas clave de diseño:
- La LWMA trabaja sobre el precio ponderado `(High + Low + 2 * Close) / 4`, coincidiendo con el modo `PRICE_WEIGHTED` de MetaTrader.
- El CCI usa el precio típico `(High + Low + Close) / 3`, como en `PRICE_TYPICAL`.
- Todas las decisiones se evalúan en la barra recién cerrada, lo que refleja el EA original que esperaba al inicio de la siguiente barra antes de actuar sobre la anterior.
- La protección de posición replica el take-profit y stop-loss del EA en unidades de pip.

## Reglas de trading
1. **Entradas largas**
   - Precio de cierre por encima de la LWMA y CCI por debajo de `CciLevelBuy` (por defecto -100), *o bien*
   - Precio de cierre por debajo de la LWMA y CCI por debajo de `CciLevelDown` (por defecto -200).
   - Entrar solo si la posición neta actual es plana o corta.
2. **Entradas cortas**
   - Precio de cierre por debajo de la LWMA y CCI por encima de `CciLevelSell` (por defecto 100), *o bien*
   - Precio de cierre por encima de la LWMA y CCI por encima de `CciLevelUp` (por defecto 200).
   - Entrar solo si la posición neta actual es plana o larga.
3. **Filtro de tiempo**
   - Cuando `UseTimeFilter` está habilitado, la estrategia verifica la hora de `candle.CloseTime`.
   - Si la hora está fuera de la ventana activa, todas las posiciones y órdenes se cancelan/cierran inmediatamente.
4. **Controles de riesgo**
   - `StartProtection` establece distancias absolutas de stop-loss y take-profit usando el tamaño de pip derivado de `Security.PriceStep`.
   - El volumen de la orden se neta de modo que abrir en la dirección opuesta cierra automáticamente la exposición anterior.

## Parámetros
| Nombre | Descripción | Por defecto |
| ---- | ----------- | ------- |
| `OrderVolume` | Tamaño del trade en lotes. | `1` |
| `StopLossPips` | Distancia de stop-loss expresada en pips (0 deshabilita). | `150` |
| `TakeProfitPips` | Distancia de take-profit en pips (0 deshabilita). | `150` |
| `UseTimeFilter` | Habilita el filtro de sesión. | `true` |
| `StartHour` | Hora de inicio de la sesión en tiempo de exchange (0-23). | `10` |
| `EndHour` | Hora de fin de la sesión en tiempo de exchange (0-23). Cuando es menor que la hora de inicio, la sesión cruza la medianoche. | `5` |
| `CciPeriod` | Longitud del Commodity Channel Index. | `14` |
| `CciLevelUp` | Umbral corto agresivo (+200). | `200` |
| `CciLevelDown` | Umbral largo agresivo (-200). | `-200` |
| `CciLevelBuy` | Umbral largo suave cuando el precio está por encima de la MA (-100). | `-100` |
| `CciLevelSell` | Umbral corto suave cuando el precio está por debajo de la MA (+100). | `100` |
| `MaPeriod` | Longitud de la LWMA. | `200` |
| `MaShift` | Desplazamiento horizontal de la LWMA en barras. La vela actual se compara con el valor `MaShift` barras atrás. | `0` |
| `CandleType` | Tipo de datos de vela/marco temporal usado para los cálculos. | `1 hour time frame` |

## Detalles de implementación
- **Cálculo de pip** – El tamaño de pip es igual a `Security.PriceStep`. Para símbolos forex de 3 o 5 decimales se multiplica por 10 para traducir 0.00001 al pip 0.0001 usado por el EA.
- **Filtro de sesión** – Implementa los dos escenarios del código fuente MQL: ventanas intradía (`StartHour < EndHour`) y ventanas nocturnas (`StartHour > EndHour`). Cuando `StartHour == EndHour`, el trading está deshabilitado, coincidiendo con la lógica original.
- **Vinculación de indicadores** – Usa `SubscribeCandles().Bind(...)` para que el CCI y la LWMA reciban actualizaciones automáticas sin buffering manual. Los valores se almacenan solo para soportar el desplazamiento opcional de la LWMA.
- **Gestión de órdenes** – `CancelActiveOrders()` se ejecuta antes de cada orden de mercado, reflejando el comportamiento del EA de mantener un libro de órdenes limpio.
- **Sin versión Python** – Solo se proporciona la estrategia C#, según lo solicitado.

## Uso
1. Adjuntar la estrategia a un instrumento y configurar `CandleType` al marco temporal deseado.
2. Elegir el volumen y los parámetros de pip apropiados para el instrumento (recordar alinear las definiciones de pip del broker con la conversión incorporada).
3. Habilitar o deshabilitar el filtro de sesión según las horas de trading.
4. Iniciar la estrategia; se suscribirá a velas, aplicará la lógica del indicador y gestionará órdenes/stops automáticamente.
