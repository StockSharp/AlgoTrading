# Estrategia de Histograma XRSI DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
Esta estrategia replica el asesor experto **Exp_XRSIDeMarker_Histogram**. Opera reversiones detectadas por un oscilador personalizado que combina un Índice de Fuerza Relativa (RSI) con el indicador DeMarker y luego suaviza el resultado. El sistema puede abrir o cerrar operaciones largas y cortas de forma independiente, y se admiten stops protectores opcionales expresados en pasos de precio.

## Construcción del indicador
1. **Precio aplicado** – el RSI se calcula sobre la entrada seleccionada (precio de cierre, apertura, máximo, mínimo, mediano, típico o ponderado) usando el período configurado.
2. **Componente DeMarker** – para cada vela terminada, la estrategia mide la presión alcista (`deMax`) y bajista (`deMin`):
   - `deMax = max(High_t - High_{t-1}, 0)`
   - `deMin = max(Low_{t-1} - Low_t, 0)`
   Ambas series se suavizan con una media móvil simple cuya longitud coincide con el período del RSI.
   - `DeMarker = deMaxAvg / (deMaxAvg + deMinAvg)` (escalado al rango 0–100).
3. **Oscilador compuesto** – el valor final es `(RSI + 100 * DeMarker) / 2`.
4. **Suavizado** – el oscilador compuesto pasa por una de las medias móviles soportadas (SMA, EMA, SMMA, LWMA o Jurik). Si se selecciona un modo no soportado de la versión MQL original, el indicador recurre a una EMA con la longitud solicitada. La opción Jurik también respeta el parámetro de fase.
5. **Historial de señales** – la estrategia almacena valores históricos y evalúa señales en la barra definida por `SignalBar`, imitando el EA original que esperaba la siguiente vela antes de operar.

## Lógica de trading
- **Reversión alcista**
  - Condición: el valor en `SignalBar+1` es menor que en `SignalBar+2` (pendiente descendente) y el valor en `SignalBar` vuelve a subir (`>=`).
  - Acciones:
    - Cerrar operaciones cortas existentes cuando `CloseShortOnLongSignal` es verdadero.
    - Abrir una nueva operación larga con `TradeVolume` (más la cantidad necesaria para invertir desde un corto) cuando `AllowBuyEntries` está habilitado.
- **Reversión bajista**
  - Condición: el valor en `SignalBar+1` es mayor que en `SignalBar+2` (pendiente ascendente) y el valor en `SignalBar` baja (`<=`).
  - Acciones:
    - Cerrar operaciones largas existentes cuando `CloseLongOnShortSignal` es verdadero.
    - Abrir una nueva operación corta cuando `AllowSellEntries` está habilitado.
- Las señales se ignoran hasta que el indicador y los componentes DeMarker estén completamente formados, y las órdenes se colocan solo cuando la estrategia está en línea y se permite el trading.

## Gestión de riesgos
- `StopLossTicks` y `TakeProfitTicks` representan distancias en **pasos de precio**. La estrategia multiplica estos valores por `Security.PriceStep` (usando `1` si el paso del instrumento es desconocido) y cierra la posición cuando se alcanza la distancia dentro del rango de la vela.
- Pasar `0` desactiva la protección respectiva.
- El parámetro `TradeVolume` se usa como tamaño de orden predeterminado y también para calcular reversiones (la posición opuesta se cierra antes de abrir una nueva).

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `TradeVolume` | Volumen al abrir nuevas posiciones. | `0.1` |
| `StopLossTicks` | Stop protector en pasos de precio. | `1000` |
| `TakeProfitTicks` | Objetivo de beneficio en pasos de precio. | `2000` |
| `AllowBuyEntries` | Habilitar/deshabilitar entradas largas. | `true` |
| `AllowSellEntries` | Habilitar/deshabilitar entradas cortas. | `true` |
| `CloseLongOnShortSignal` | Cerrar largos cuando aparece una señal corta. | `true` |
| `CloseShortOnLongSignal` | Cerrar cortos cuando aparece una señal larga. | `true` |
| `CandleType` | Marco temporal usado para el análisis (velas de 4 horas por defecto). | `H4` |
| `IndicatorPeriod` | Retrospectiva para los componentes RSI y DeMarker. | `14` |
| `AppliedPriceSelection` | Precio aplicado usado por el cálculo del RSI. | `Close` |
| `SmoothingMethodSelection` | Media móvil usada para el suavizado (SMA/EMA/SMMA/LWMA/Jurik/Adaptive). | `Sma` |
| `SmoothingLength` | Período de la media de suavizado. | `5` |
| `SmoothingPhase` | Argumento de fase pasado al suavizado Jurik. | `15` |
| `SignalBar` | Número de barras cerradas atrás usadas para la evaluación de señales. | `1` |

## Notas vs. EA original
- Los modos de gestión monetaria de la versión MQL (basado en balance, margen libre, etc.) se reemplazan con un parámetro directo `TradeVolume`.
- El deslizamiento de órdenes (`Deviation`) no es necesario porque StockSharp usa órdenes de mercado.
- Los algoritmos de suavizado avanzados (MA Parabólica, T3, VIDYA, AMA) no están disponibles en StockSharp y se mapean a la EMA mediante la opción `Adaptive`.
- Todos los comentarios en el código fuente C# están escritos en inglés, y la lógica solo se ejecuta en velas terminadas, igual que la implementación original.
