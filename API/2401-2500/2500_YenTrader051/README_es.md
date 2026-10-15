# Estrategia YenTrader051 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia YenTrader051 replica el asesor experto original de MetaTrader que arbitra la relación entre tres pares de divisas:

- **Cruz negociada** – el instrumento que aloja la instancia de la estrategia (por ejemplo GBPJPY).
- **Par principal** – típicamente la moneda base de la cruz contra USD (por ejemplo GBPUSD).
- **USDJPY** – usado para confirmar el tramo del yen del triángulo.

Un rompimiento en el par principal combinado con confirmación de USDJPY genera las señales de trading. Filtros opcionales de RSI, CCI, RVI y media móvil refinan las entradas. La gestión de posiciones soporta tanto el promediado como el piramidado, mientras que la gestión del riesgo reproduce el manejo de stops basado en pip/ATR del EA.

## Lógica de trading

1. **Detección de rompimiento**
   - `LoopBackBars` controla la ventana de lookback. Cuando es mayor a 1, la estrategia verifica:
     - máximos/mínimos recientes (`PriceReference = HighLow`), o
     - cierres de `LoopBackBars` barras atrás (`PriceReference = Close`).
   - `MajorDirection` define cómo el par principal y el tramo del yen deben moverse en relación entre sí cuando la cruz se cotiza como principal/yen (Left) o yen/principal (Right).
2. **Filtros de entrada**
   - `UseRsiFilter` requiere RSI por encima/debajo de 50 dependiendo del alineamiento de tendencia esperado.
   - `UseCciFilter` obliga a que el CCI sea positivo/negativo.
   - `UseRviFilter` espera a que el RVI cruce su línea de señal. La línea de señal es una SMA de 4 períodos de los valores del RVI, igual que en la implementación de MT4.
   - `UseMovingAverageFilter` mantiene las entradas alineadas con una media móvil configurable (`MaMode`, `MaPeriod`).
3. **Estilo de entrada**
   - `EntryMode = Both` permite cualquier rompimiento.
   - `EntryMode = Pyramiding` solo agrega en velas alcistas/bajistas en la dirección de la operación.
   - `EntryMode = Averaging` solo agrega cuando la vela anterior cerró en contra de la posición para promediar.
4. **Dimensionamiento de órdenes**
   - `FixedLotSize` coloca un volumen constante.
   - Cuando el lote fijo es cero, la estrategia usa `BalancePercentLotSize` y el valor actual del portafolio para dimensionar las operaciones.
   - `MaxOpenPositions` limita el tamaño acumulativo (número de entradas aditivas).
5. **Gestión del riesgo**
   - Las distancias en pips (`StopLossPips`, `TakeProfitPips`, `BreakEvenPips`, `ProfitLockPips`, `TrailingStopPips`, `TrailingStepPips`) se traducen mediante `Security.MinPriceStep`.
   - Cuando `EnableAtrLevels` está activo, las distancias ATR reemplazan los pips usando el ATR diario (`AtrCandleType`, `AtrPeriod`) y los multiplicadores respectivos.
   - Stops, take-profits, break-even, bloqueo de ganancias y niveles de trailing se actualizan desde velas completadas, igual que en la implementación MQL.
   - `CloseOnOpposite` cerrará las posiciones existentes en lugar de apilar nuevas cuando aparezca un rompimiento opuesto.
   - `AllowHedging` permite a la estrategia agregar a una posición incluso si hay una posición contraria aún abierta. Ten en cuenta que las estrategias de StockSharp usan posiciones netas, por lo que las posiciones simultáneas larga/corta no son compatibles; el flag controla efectivamente si la estrategia puede aumentar la exposición cuando la posición neta actual apunta en la otra dirección.

## Parámetros

| Grupo | Nombre | Descripción |
|-------|--------|-------------|
| Instrumentos | `MajorSecurity` | Par principal usado para confirmación de rompimiento. |
| | `UsdJpySecurity` | Instrumento USDJPY para confirmación del tramo del yen. |
| Datos | `CandleType` | Marco temporal de señal para los tres pares. |
| Filtros | `MajorDirection` | Alineación entre el par principal y la cruz negociada (Left = principal/yen, Right = yen/principal). |
| | `PriceReference` | Rompimiento de alto/bajo o comparación de cierre diferido. |
| | `LoopBackBars` | Número de barras históricas para evaluar el rompimiento. |
| | `EntryMode` | Promediado, piramidado o ambos. |
| Indicadores | `UseRsiFilter`, `UseCciFilter`, `UseRviFilter`, `UseMovingAverageFilter` | Activar/desactivar filtros de confirmación adicionales. |
| | `MaPeriod`, `MaMode` | Configuración de media móvil. |
| Riesgo | `FixedLotSize`, `BalancePercentLotSize` | Controles de volumen. |
| | `MaxOpenPositions` | Número máximo de entradas aditivas. |
| | `StopLossPips`, `TakeProfitPips`, `BreakEvenPips`, `ProfitLockPips`, `TrailingStopPips`, `TrailingStepPips` | Distancias de riesgo basadas en pips. |
| | `EnableAtrLevels`, `AtrCandleType`, `AtrPeriod`, `AtrStopLossMultiplier`, `AtrTakeProfitMultiplier`, `AtrTrailingMultiplier`, `AtrBreakEvenMultiplier`, `AtrProfitLockMultiplier` | Configuración de riesgo basada en ATR. |
| Comportamiento | `CloseOnOpposite` | Cerrar o invertir posiciones en señales opuestas. |
| | `AllowHedging` | Permitir entradas cuando existe una posición neta contraria. |

## Notas de uso

- Asigna el instrumento de la cruz negociada a la propiedad `Security` de la estrategia, luego establece `MajorSecurity` y `UsdJpySecurity` para los instrumentos de soporte.
- Asegúrate de que el portafolio esté conectado; el dimensionamiento de lotes variables requiere `Portfolio.CurrentValue`.
- La estrategia espera datos de velas sincronizados para los tres instrumentos. Si diferentes bolsas entregan datos con calendarios de sesión diferentes, considera resamplear a un marco temporal común.
- Los cálculos de ATR se suscriben al `AtrCandleType` configurado. Mantenlo alineado con los valores predeterminados del EA original (diario, 21 períodos) para un comportamiento comparable.
- La lógica de riesgo opera en velas cerradas, por lo que las órdenes de protección se ejecutan mediante salidas de mercado cuando los umbrales se violan durante la vela subsiguiente.

## Diferencias vs. la versión MT4

- StockSharp usa posiciones netas agregadas; el verdadero hedging (mantener largo y corto simultáneamente) no está disponible. `AllowHedging` simplemente controla si la estrategia puede invertir posiciones automáticamente cuando aparece una nueva señal.
- La gestión de stop/límite se implementa con salidas de mercado después de que los umbrales se activan en los datos de velas. El EA original modifica los stops de las órdenes directamente porque opera a nivel de tick.
- La línea de señal del RVI se implementa como una SMA de cuatro períodos de los valores del RVI, coincidiendo con el comportamiento de `MODE_SIGNAL` en MT4.
