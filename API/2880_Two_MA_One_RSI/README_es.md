# Estrategia Two MA One RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el experto de MetaTrader 5 "Two MA one RSI" a StockSharp. Combina el cruce de una media móvil rápida y lenta con una confirmación RSI evaluada en la vela cerrada anterior. Interruptores flexibles permiten convertir cada comparación en una regla "mayor que" o "menor que", de modo que la configuración puede invertirse sin tocar el código.

## Detalles
- **Criterios de entrada**:
  - Las señales largas requieren que la MA rápida esté por debajo de la MA lenta hace dos barras, que la MA rápida esté por encima de la MA lenta en la barra cerrada más reciente, y que el RSI de la barra anterior esté por encima del umbral superior. Cada comparación puede invertirse mediante parámetros booleanos.
  - Las señales cortas reflejan la lógica y comprueban las relaciones MA opuestas junto con el RSI cayendo por debajo del umbral inferior.
  - Ambas MAs usan el mismo tipo de promedio; el período lento siempre es `FastMaPeriod * SlowPeriodMultiplier`. Los desplazamientos horizontales opcionales reproducen el comportamiento de MT5 donde los valores del indicador se leen varias velas atrás.
- **Largo/Corto**: La estrategia puede abrir posiciones en ambas direcciones. `CloseOppositePositions` controla si una nueva señal fuerza el cierre del lado opuesto antes de entrar.
- **Criterios de salida**:
  - Stop-loss y take-profit configurables en pips.
  - Trailing stop opcional que solo se mueve después de que el precio avanza al menos `TrailingStopPips + TrailingStepPips` más allá de la entrada.
  - `ProfitClose` monitoriza el P&L flotante (usando el precio de paso del instrumento) y cierra todas las posiciones una vez alcanzado el importe objetivo en divisa.
- **Stops**: Cuando `StopLossPips` es cero, la estrategia depende puramente del módulo de trailing stop (si está habilitado). `TrailingStopPips` requiere un `TrailingStepPips` positivo, coincidiendo con la validación del experto original.
- **Valores predeterminados**:
  - `FastMaPeriod = 10`, `SlowPeriodMultiplier = 2`.
  - `FastMaShift = 3`, `SlowMaShift = 0`.
  - `RsiPeriod = 10`, `RsiUpperLevel = 70`, `RsiLowerLevel = 30`.
  - `StopLossPips = 50`, `TakeProfitPips = 150`, `TrailingStopPips = 15`, `TrailingStepPips = 5`.
  - `MaxPositions = 10`, `ProfitClose = 100`, `TradeVolume = 1`.
- **Filtros**: Seis interruptores booleanos (`BuyPreviousFastBelowSlow`, `BuyCurrentFastAboveSlow`, `BuyRequiresRsiAboveUpper`, `SellPreviousFastAboveSlow`, `SellCurrentFastBelowSlow`, `SellRequiresRsiBelowLower`) permiten al usuario cambiar instantáneamente el sentido de cada comparación.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal (u otro tipo de vela) utilizado para el análisis. |
| `MaType` | Familia de medias móviles (simple, exponencial, suavizada, ponderada, ponderada por volumen). |
| `FastMaPeriod` | Período de la MA rápida. |
| `SlowPeriodMultiplier` | Multiplicador de período de la MA lenta (`lenta = rápida * multiplicador`). |
| `FastMaShift`, `SlowMaShift` | Desplazamientos horizontales en velas aplicados al evaluar los valores de MA. |
| `RsiPeriod` | Longitud del RSI (usa la vela finalizada anterior). |
| `RsiUpperLevel`, `RsiLowerLevel` | Umbrales RSI para confirmaciones largas y cortas. |
| `BuyPreviousFastBelowSlow`, `BuyCurrentFastAboveSlow`, `BuyRequiresRsiAboveUpper` | Activar/desactivar comparaciones para señales largas. |
| `SellPreviousFastAboveSlow`, `SellCurrentFastBelowSlow`, `SellRequiresRsiBelowLower` | Activar/desactivar comparaciones para señales cortas. |
| `StopLossPips`, `TakeProfitPips` | Stop protector y objetivo medido en pips (tamaño del pip derivado del paso de precio del instrumento). |
| `TrailingStopPips`, `TrailingStepPips` | Distancia del trailing stop y mejora mínima. |
| `MaxPositions` | Número máximo de entradas simultáneas por dirección (`0` = ilimitado). |
| `ProfitClose` | Objetivo de beneficio en divisa que cierra todas las posiciones al alcanzarse. |
| `CloseOppositePositions` | Si se debe cerrar el lado opuesto antes de abrir una nueva operación. |
| `TradeVolume` | Tamaño base de la orden; también se sincroniza con la propiedad `Volume` de la estrategia. |

## Notas de implementación
- Todas las decisiones usan solo velas finalizadas, igualando la lógica de "nueva barra" del experto MT5.
- El tamaño del pip es igual al paso de precio del instrumento. Si su mercado usa precios de pip fraccionarios, ajuste la configuración del instrumento en consecuencia para que la traducción del pip coincida con la lógica `digits_adjust` del experto original.
- Los trailing stops solo comienzan después de que el precio ha avanzado `TrailingStopPips + TrailingStepPips`; el stop entonces se ancla `TrailingStopPips` lejos del cierre y solo se mueve cuando mejora al menos `TrailingStepPips`.
- `ProfitClose` calcula el beneficio flotante usando `PriceStep` y `StepPrice` del instrumento. Asegúrese de que esos campos estén configurados para resultados de divisa correctos.
