# Estrategia RAVI + Awesome Oscillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Port del asesor experto de MetaTrader 5 "Ravi AO (edición de barabashkakvn)" a la API de alto nivel de StockSharp.
- Combina el Range Action Verification Index (RAVI) con el Awesome Oscillator (AO) para detectar cambios de impulso alcista y bajista sincronizados.
- Funciona en cualquier marco temporal e instrumento compatible con StockSharp; todos los ajustes numéricos se expresan en pips para mantenerse cerca de la implementación original.

## Indicadores
- **RAVI** – calculado como `100 * (FastMA - SlowMA) / SlowMA` en la serie de precios seleccionada. Puede elegir el método de suavizado (simple, exponencial, suavizado, ponderado), las longitudes y la fuente de precio (cierre, apertura, máximo, mínimo, mediano, típico, ponderado, simple, cuarto, trend-follow, Demark).
- **Awesome Oscillator** – indicador de impulso de precio mediano con períodos corto y largo configurables. Los valores predeterminados coinciden con los de MT5 (5 y 34).

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal de la vela o tipo de datos para suscribirse. |
| `StopLossPips` | Distancia de stop-loss de protección en pips. `0` deshabilita el stop. |
| `TakeProfitPips` | Distancia de take-profit en pips. `0` deshabilita el take profit. |
| `TrailingStopPips` | Distancia base del trailing stop en pips. `0` deshabilita el trailing. |
| `TrailingStepPips` | Beneficio adicional mínimo (en pips) requerido antes de que el trailing stop se ajuste. Debe ser > 0 cuando el trailing está habilitado. |
| `FastMethod` / `FastLength` | Método de suavizado y longitud de la media móvil rápida del RAVI. |
| `SlowMethod` / `SlowLength` | Método de suavizado y longitud de la media móvil lenta del RAVI. |
| `AppliedPrices` | Fórmula de precio utilizada por ambas medias del RAVI (cierre, apertura, máximo, mínimo, mediano, típico, ponderado, simple, cuarto, trend-follow #1/#2, Demark). |
| `AoShortPeriod` / `AoLongPeriod` | Períodos rápido y lento del Awesome Oscillator. |

## Reglas de Trading
1. La estrategia actualiza los indicadores cuando una vela cierra (`CandleStates.Finished`).
2. Una **entrada alcista** se activa cuando:
   - AO hace dos barras `< 0` y AO hace una barra `> 0` (cruce positivo de cero), y
   - RAVI hace dos barras `< 0` y RAVI hace una barra `> 0`.
3. Una **entrada bajista** se activa cuando:
   - AO hace dos barras `> 0` y AO hace una barra `< 0`, y
   - RAVI hace dos barras `> 0` y RAVI hace una barra `< 0`.
4. Solo puede haber una posición abierta a la vez. Las señales se ignoran mientras exista una posición.

## Gestión de Salidas
- **Stop-loss**: calculado a partir de `StopLossPips` usando el paso de precio del instrumento (los símbolos FX de 5 y 3 dígitos usan un multiplicador de 10×, coincidiendo con la definición de pip de MT5). Se activa cuando los extremos de la vela alcanzan el nivel del stop.
- **Take-profit**: objetivo opcional calculado de la misma manera; deshabilitado cuando `TakeProfitPips = 0`.
- **Trailing stop**: cuando está habilitado, el stop se ajusta una vez que el beneficio flotante supera `TrailingStopPips + TrailingStepPips`. Para largos el stop se mueve a `ClosePrice - TrailingStopPips`; para cortos a `ClosePrice + TrailingStopPips`.
- Todas las salidas cierran la posición completa con órdenes de mercado.

## Notas de Implementación
- Las señales se evalúan al cierre de la barra; las entradas reales ocurren en el mismo cierre de la vela, mientras que la versión MT5 entra en la apertura de la siguiente barra. Ajusta los ajustes si necesitas compensar esta diferencia.
- Solo se usan las medias móviles proporcionadas por StockSharp; los modos de suavizado exóticos de la biblioteca MT5 (JJMA, Jurik, T3, etc.) no están disponibles.
- El parámetro visual `Shift` del indicador MT5 solo afecta la representación gráfica; no tiene impacto en el trading y por tanto se omite.
- Las fórmulas de `AppliedPrices` siguen las definiciones de MetaTrader, incluyendo las opciones TrendFollow y Demark.

## Consejos de Uso
- La estrategia es de seguimiento de tendencia; combínala con filtros de marco temporal superior o filtros de volatilidad para reducir las señales falsas.
- Optimiza las longitudes y las distancias en pips por instrumento, especialmente al cambiar entre FX, CFDs y futuros, porque el tamaño del pip se deriva de `Security.PriceStep`.
- Habilita `Strategy.StartProtection` externamente si deseas órdenes de stop del lado del broker en lugar de salidas dentro de la estrategia.
