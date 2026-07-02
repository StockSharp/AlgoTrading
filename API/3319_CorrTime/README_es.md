# Estrategia CorrTime
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia CorrTime es un sistema de un solo símbolo que replica el asesor experto de MetaTrader del mismo nombre. Analiza la correlación entre precios de cierre y su orden cronológico para detectar aceleración o reversión del momentum. El algoritmo opera sobre velas completadas y combina tres capas de confirmación:

1. **Filtro de volatilidad:** la anchura de las bandas de Bollinger debe situarse dentro de una banda configurable de actividad aceptable, por lo que el sistema evita fases planas y excesivamente volátiles.
2. **Filtro de fuerza de tendencia:** el Average Directional Index (ADX) debe permanecer por encima de un umbral antes de evaluar señales de correlación.
3. **Disparadores de correlación:** estimadores de correlación Pearson, Spearman, Kendall o Fechner miden qué tan estrechamente evoluciona el precio con el tiempo. Un cambio repentino del coeficiente genera la decisión de trading.

Aunque el robot original fue diseñado para EURUSD en H1, la versión StockSharp mantiene todos los parámetros configurables. Los valores predeterminados siguen fieles a la fuente (velas de 1 hora, correlación Fechner, modo de trading inverso).

## Flujo de negociación

1. Suscribirse al `CandleType` seleccionado y esperar una barra terminada.
2. Actualizar valores de bandas de Bollinger y ADX en la nueva vela.
3. Rechazar la barra cuando:
   - El spread de Bollinger convertido a pips está fuera de `[BollingerSpreadMin, BollingerSpreadMax]`.
   - ADX está por debajo de `AdxLevel`.
   - La vela comienza fuera de la ventana `[EntryHour, EntryHour + OpenHours]` (con soporte para cruce de medianoche).
4. Construir una historia móvil de precios de cierre y calcular el coeficiente de correlación sobre los lookbacks `CorrelationRangeTrend` y `CorrelationRangeReverse`. El código recalcula los tres últimos valores de correlación para detectar un cruce real de límites, exactamente como lo hacía el archivo include original con búferes.
5. Disparador seguidor de tendencia (cuando `TradeMode` es *TrendFollow* o *Both*):
   - **Largo:** la correlación estaba por debajo de `CorrLimitTrendBuy`, seguía debajo en la barra previa y cruza por encima del umbral en la última barra.
   - **Corto:** la correlación estaba por encima de `-CorrLimitTrendSell`, seguía encima en la barra previa y cruza por debajo de `-CorrLimitTrendSell` en la última barra.
6. Disparador de reversión (cuando `TradeMode` es *Reverse* o *Both*):
   - **Largo:** la correlación estaba por debajo de `-CorrLimitReverseBuy`, seguía debajo en la barra previa y sube por encima de `-CorrLimitReverseBuy` en la última barra.
   - **Corto:** la correlación estaba por encima de `CorrLimitReverseSell`, seguía encima en la barra previa y cae por debajo de `CorrLimitReverseSell` en la última barra.
7. Si ambas direcciones se activan simultáneamente, las señales se cancelan entre sí, reflejando el comportamiento de MetaTrader.
8. Si `CloseTradeOnOppositeSignal` está activado, la estrategia cierra de inmediato cualquier posición opuesta antes de abrir una nueva.
9. Las entradas se dimensionan con la propiedad `Volume` y respetan `MaxOpenOrders`, por lo que la exposición neta nunca supera `Volume * MaxOpenOrders` en ninguna dirección.
10. El riesgo se controla mediante `StartProtection`: stop-loss y take-profit usan distancias en pips, y la bandera de trailing reutiliza la misma distancia de stop si está activada.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco usado para generar velas y alimentar todos los indicadores. |
| `CloseTradeOnOppositeSignal` | Cierra posiciones abiertas cuando la siguiente señal apunta en la dirección opuesta. |
| `EntryHour`, `OpenHours` | Define la ventana diaria de trading. `OpenHours = 0` mantiene la ventana abierta por una sola hora. |
| `BollingerPeriod`, `BollingerDeviation` | Ajustes estándar de bandas de Bollinger aplicados a cierres. |
| `BollingerSpreadMin`, `BollingerSpreadMax` | Anchura mínima y máxima (en pips) requerida para el canal Bollinger. |
| `AdxPeriod`, `AdxLevel` | Configuración ADX y fuerza mínima de tendencia requerida. |
| `TradeMode` | Elige entre seguimiento de tendencia, reversión o evaluación combinada. |
| `CorrelationRangeTrend`, `CorrelationRangeReverse` | Longitudes de lookback para cálculos de correlación. |
| `CorrelationType` | Selecciona fórmulas de correlación Pearson, Spearman, Kendall o Fechner. |
| `CorrLimitTrendBuy`, `CorrLimitTrendSell` | Umbrales que definen una ruptura válida seguidora de tendencia. |
| `CorrLimitReverseBuy`, `CorrLimitReverseSell` | Umbrales que definen una ruptura válida de reversión. |
| `TakeProfitPips`, `StopLossPips`, `TrailingStopPips` | Parámetros de riesgo expresados en pips y convertidos a unidades de precio con el tamaño de pip del instrumento. |
| `MaxOpenOrders` | Límite superior del número agregado de entradas (tope por lado igual a `Volume * MaxOpenOrders`). |

## Notas prácticas

- El tamaño de pip se deduce de los decimales del instrumento (5 o 3 decimales corresponden a un multiplicador 10x) para imitar el manejo de puntos de MetaTrader. Ajuste los umbrales al trabajar con activos no forex.
- Los búferes de correlación necesitan al menos `lookback + 2` velas completadas para evaluar un cruce. Durante el calentamiento la estrategia permanece inactiva.
- Como toda la lógica se ejecuta en velas terminadas, la estrategia es resistente al ruido intrabar y refleja el comportamiento original basado en snapshots `iTime` e `iClose`.
- Combine esta estrategia con controles de riesgo de cartera al desplegar múltiples instancias, ya que el robot original también limitaba el número total de órdenes entre símbolos.
