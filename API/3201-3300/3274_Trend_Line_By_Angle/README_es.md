# Estrategia de línea de tendencia por ángulo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia es una adaptación a StockSharp del asesor experto de MetaTrader *Trend Line By Angle*. El robot original mezclaba entradas manuales por botones con herramientas extensas de gestión monetaria. Esta versión convierte el flujo discrecional en un sistema automatizado de seguimiento de tendencia con MACD, preservando la lógica de protección:

- MACD mensual (12/26/9), calculado sobre el tipo de vela de señal configurado, define la dirección. Los cruces alcistas abren exposición larga; los cruces bajistas abren exposición corta.
- Las entradas escalan hasta el número configurado de bloques, reflejando los clics manuales repetidos del EA fuente.
- Bollinger Bands (20, 2) vigilan el marco temporal de ejecución. Tocar la banda superior liquida la exposición larga; tocar la banda inferior liquida los cortos, replicando los botones visuales de stop de MetaTrader.
- Controles de riesgo clásicos - stop-loss, take-profit, trailing stop y movimiento a break-even - operan sobre distancias en pips convertidas mediante el `PriceStep` del instrumento.
- La protección a nivel de cuenta cierra todas las órdenes cuando se alcanza un objetivo de ganancia monetario o porcentual. Un bloqueo trailing adicional basado en dinero sigue la ganancia flotante y sale con el drawdown configurado.

## Flujo de ejecución

1. **Preparación de indicadores** - `MovingAverageConvergenceDivergenceSignal` se ejecuta sobre `SignalCandleType`, mientras que `BollingerBands` se ejecuta sobre el `CandleType` de trading.
2. **Señales de entrada** - En cada vela de ejecución cerrada se evalúa el último cruce MACD. Un cruce al alza dispara `BuyMarket`; un cruce a la baja dispara `SellMarket`. La exposición opuesta existente se cierra antes de invertir.
3. **Lógica de escalado** - La estrategia sigue comprando/vendiendo hasta que la posición agregada alcanza `TradeVolume * MaxEntries`.
4. **Gestión de riesgos** - Los niveles de break-even, trailing stop, stop-loss y take-profit se recalculan en cada vela. Un toque de Bollinger fuerza una salida incluso si no se han alcanzado otros niveles.
5. **Protección de cuenta** - Las comprobaciones de take-profit monetario y porcentual se ejecutan antes de generar nuevas señales. El módulo de trailing monetario rastrea el PnL total más alto y cierra todo cuando la caída supera `MoneyTrailStop`.

## Detalles de gestión monetaria

- **PnL total** es la suma de la ganancia realizada (`PnL`) y el PnL flotante calculado desde el precio de cierre de la vela, el paso de precio y el valor del paso.
- **Break-even** mueve el stop de protección a `Entry + BreakEvenOffsetPips` (largo) o `Entry - BreakEvenOffsetPips` (corto) cuando el movimiento supera `BreakEvenTriggerPips`.
- **Trailing stop** se desplaza más cerca del precio cuando la ganancia supera `TrailingStopPips`. Los niveles trailing largos solo aumentan; los niveles trailing cortos solo disminuyen.
- **Trailing monetario** se activa después de ver una ganancia de `MoneyTrailTrigger`. A partir de entonces se memoriza la ganancia más alta; perder más de `MoneyTrailStop` desde ese pico cierra todas las posiciones.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `TradeVolume` | Volumen de cada bloque de entrada. |
| `MaxEntries` | Número máximo de bloques de volumen que se pueden acumular. |
| `StopLossPips` | Distancia del stop-loss en pips. |
| `TakeProfitPips` | Distancia del take-profit en pips. |
| `TrailingStopPips` | Distancia trailing en pips. |
| `UseBreakEven` | Habilita el movimiento del stop a break-even. |
| `BreakEvenTriggerPips` | Ganancia necesaria antes de activar break-even. |
| `BreakEvenOffsetPips` | Pips extra añadidos al mover a break-even. |
| `UseBollingerExit` | Habilita salidas por toques de Bollinger band. |
| `BollingerPeriod` / `BollingerDeviation` | Configuración de Bollinger Bands. |
| `UseProfitMoneyTarget` / `ProfitMoneyTarget` | Interruptor y valor del objetivo absoluto de ganancia. |
| `UseProfitPercentTarget` / `ProfitPercentTarget` | Interruptor y valor del objetivo porcentual de ganancia. |
| `EnableMoneyTrail` | Habilita el trailing stop monetario. |
| `MoneyTrailTrigger` | Ganancia necesaria antes de que el trailing monetario se active. |
| `MoneyTrailStop` | Drawdown permitido desde el pico antes de salir. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuración MACD. |
| `CandleType` | Marco temporal de ejecución. |
| `SignalCandleType` | Marco temporal usado para la señal MACD. |

## Notas de uso

- La estrategia depende de valores correctos de `PriceStep` y `StepPrice` del instrumento. Configure el instrumento antes de iniciarla.
- Si la cuenta no informa el valor de la cartera (`Portfolio.CurrentValue` o `Portfolio.BeginValue`), el take-profit porcentual se ignora automáticamente.
- Todos los comentarios dentro del archivo C# documentan la lógica de trading en inglés para simplificar el mantenimiento posterior.
