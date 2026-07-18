# Macd Pattern Trader v02 (puerto StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión API de alto nivel de StockSharp del experto MetaTrader **MacdPatternTraderv02.mq4** (directorio `MQL/8194`). Reproduce la detección de patrones MACD original y las reglas de gestión de posición activa al tiempo que expone parámetros convenientes para una mayor optimización.

## Idea central

1. Calcule la línea principal MACD usando los períodos EMA rápido y lento (`FastEmaPeriod`, `SlowEmaPeriod`) con una longitud de señal de una vela (que coincide con la versión MQL).
2. Supervise únicamente las velas completadas. Cuando el valor MACD pinte una secuencia específica de tres barras alrededor de la línea cero, active el patrón corto o largo:
   - **Patrón corto**: requiere una fase MACD positiva seguida de un retroceso negativo por encima de `MinThreshold` y luego una inflexión a la baja.
   - **Patrón largo**: requiere una fase MACD negativa seguida de un retroceso positivo por debajo de `MaxThreshold` y luego una inflexión alcista.
3. Ejecute órdenes de mercado usando `TradeVolume` una vez que se confirme el patrón.
4. Proteja cada posición con un stop-loss colocado más allá del extremo de oscilación reciente (sobre `StopLossBars` velas) más una compensación adicional en puntos (`OffsetPoints`).
5. Defina el nivel de obtención de beneficios escaneando `TakeProfitBars` segmentos consecutivos y seleccionando el máximo/mínimo más extremo alcanzado mientras la secuencia sigue imprimiendo nuevos extremos.
6. Administre las posiciones abiertas con el administrador de posiciones activo del experto original: después de lograr una ganancia mínima de cinco puntos, la estrategia cierra un tercio del volumen cuando la vela anterior confirma la tendencia (filtro `Ema2Period`) y otra mitad cuando el precio interactúa con la línea media de `SmaPeriod` y `Ema3Period`.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `StopLossBars` | Número de velas completadas inspeccionadas al calcular el extremo de oscilación del stop-loss. |
| `TakeProfitBars` | Tamaño de ventana (en velas) para la búsqueda secuencial de extremos que construye el objetivo de obtención de beneficios. |
| `OffsetPoints` | Compensación adicional, expresada en puntos del instrumento, agregada al stop-loss. |
| `FastEmaPeriod` | Longitud rápida de EMA para la línea principal MACD. |
| `SlowEmaPeriod` | Longitud lenta de EMA para la línea principal MACD. |
| `MaxThreshold` | Umbral positivo MACD que finaliza la preparación del patrón corto. |
| `MinThreshold` | Umbral negativo MACD que finaliza la preparación del patrón largo. |
| `Ema1Period` | Primer período EMA utilizado por el bloque de administración de dinero original (se conserva para que esté completo). |
| `Ema2Period` | Segundo período EMA utilizado para validar el beneficio parcial para posiciones largas/cortas. |
| `SmaPeriod` | SMA período utilizado en el segundo activador de cierre parcial. |
| `Ema3Period` | Período EMA lenta combinado con SMA para detectar salidas de reversión a la media. |
| `TradeVolume` | Volumen de órdenes de mercado (lotes). |
| `CandleType` | Tipo de datos de vela utilizado para alimentar todos los indicadores. |

## Lógica de trading

- **Entrada corta**: se activa cuando la secuencia MACD `(prev3, prev2, prev1, current)` coincide con las condiciones originales (`macdPrev1 < macdPrev3`, `macdPrev1 > macdPrev2`, `current < prev1`, `current < 0` y verificación de magnitud). La exposición larga existente se aplana antes de abrir una nueva posición corta.
- **Entrada larga**: reglas simétricas donde `current > 0`, los valores MACD forman el patrón de imagen especular y se cumple la verificación de magnitud. La exposición corta existente se aplana antes de abrir una nueva posición larga.
- **Paradas y objetivos**: se calcula inmediatamente después de cada entrada y se actualiza solo cuando se ejecuta una nueva operación.
- **Cierres parciales**: una vez que la ganancia alcanza cinco puntos (en relación con el tamaño de puntos del instrumento), la estrategia cierra un tercio del volumen restante si la vela anterior cierra más allá de `EMA2`. La siguiente etapa cierra la mitad del volumen restante cuando la vela anterior atraviesa el promedio de `SMA` y `EMA3`.
- **Salida completa**: cualquier toque del precio en el nivel de stop-loss o take-profit cierra la posición completa. Después de cada salida forzada, el estado interno se restablece automáticamente.

## Notas

- El tamaño en puntos se deriva de `Security.PriceStep` o, cuando no esté disponible, de los decimales de seguridad. Se utiliza un valor predeterminado de `0.0001` como respaldo seguro.
- El historial de velas se almacena (hasta 1024 entradas) para replicar las funciones auxiliares MQL `iHighest`, `iLowest` y el escaneo extremo secuencial de `TakeProfit()`.
- Todos los comentarios dentro de la estrategia permanecen en inglés, como lo exigen las pautas del repositorio.
- Los puertos de Python se omiten intencionalmente para esta tarea.
