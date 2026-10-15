# Estrategia Starter Triple Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el experto de MetaTrader **Starter.mq5** a la API de alto nivel de StockSharp. Sincroniza tres osciladores stochastic (rápido, normal, lento) con medias móviles coincidentes calculadas en diferentes marcos temporales. Un trade se permite solo cuando todos los filtros confirman la misma dirección y el precio opera en el lado correcto de cada media móvil desplazada.

## Lógica de trading

1. La estrategia se suscribe a tres flujos de velas:
   - **Marco temporal rápido** (por defecto `M5`).
   - **Marco temporal normal** (por defecto `M30`).
   - **Marco temporal lento** (por defecto `H2`).
2. Para cada flujo construye una media móvil (método configurable, longitud y precio aplicado) y un oscilador stochastic con los mismos parámetros `%K`, `%D` y slowdown.
3. El marco temporal lento impulsa la ejecución. Cuando una vela lenta cierra, se comparan los últimos valores de todos los marcos temporales:
   - Configuración larga: cada línea stochastic tiene `%K > %D`, todos los valores `%K` están por debajo de `50`, y el precio está por debajo de cada media móvil desplazada.
   - Configuración corta: cada línea stochastic tiene `%K < %D`, todos los valores `%K` están por encima de `50`, y el precio está por encima de cada media móvil desplazada.
4. Las señales pueden invertirse opcionalmente a través de `ReverseSignals`. Cuando se toma una entrada, la estrategia invierte la exposición existente (si `CloseOppositePositions = true`) o ignora la señal hasta que el posición opuesta esté cerrada.
5. Después de un llenado, los niveles de stop-loss y take-profit se simulan en el espacio de precio. Un trailing stop replica la lógica MQL original requiriendo `TrailingStopPips + TrailingStepPips` de ganancia antes de mover el stop por `TrailingStopPips`.
6. El dimensionamiento de posición basado en riesgo refleja el interruptor `lot`/`risk` de MetaTrader. Cuando el modo es `RiskPercent`, el volumen del trade se deriva del valor de la cuenta, el porcentaje de riesgo y la distancia de stop-loss en pips.

## Parámetros

| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `StopLossPips` | `45` | Distancia protectora de stop en pips. Establecer en `0` para deshabilitar el stop fijo. |
| `TakeProfitPips` | `105` | Distancia de take-profit en pips. Establecer en `0` para deshabilitar el objetivo. |
| `TrailingStopPips` | `5` | Offset de trailing stop aplicado después del avance mínimo. |
| `TrailingStepPips` | `5` | Avance mínimo de ganancia (en pips) requerido antes de que el trailing stop se mueva. |
| `MoneyMode` | `RiskPercent` | Selecciona entre dimensionamiento de lote fijo y riesgo porcentual por trade. |
| `MoneyValue` | `3` | Tamaño de lote cuando se usa `FixedLot`, o porcentaje de riesgo cuando se usa `RiskPercent`. |
| `FastCandleType` | `M5` | Tipo de vela para el conjunto de indicadores rápido. |
| `NormalCandleType` | `M30` | Tipo de vela para el conjunto de indicadores intermedio. |
| `SlowCandleType` | `H2` | Tipo de vela que desencadena la evaluación de señales y órdenes. |
| `MaPeriod` | `20` | Longitud de todas las medias móviles. |
| `MaShift` | `1` | Desplazamiento horizontal aplicado a cada media móvil (barras atrás). |
| `MaMethod` | `Simple` | Suavizado de media móvil: `Simple`, `Exponential`, `Smoothed`, o `Weighted`. |
| `MaPriceType` | `Close` | Precio aplicado para alimentar las medias móviles. |
| `StochasticKPeriod` | `5` | Longitud `%K` para todos los osciladores stochastic. |
| `StochasticDPeriod` | `3` | Longitud de suavizado `%D`. |
| `StochasticSlowing` | `3` | Factor de slowdown final para `%K`. |
| `ReverseSignals` | `false` | Intercambia las condiciones larga y corta. |
| `CloseOppositePositions` | `false` | Si es `true`, revierte la posición en una única orden cuando aparece una señal en la dirección opuesta. |

## Gestión monetaria

- `MoneyMode = FixedLot` envía cada orden con el volumen exacto de `MoneyValue`.
- `MoneyMode = RiskPercent` reproduce el comportamiento original: el efectivo arriesgado equivale a `AccountValue * MoneyValue / 100`. El tamaño del trade se calcula como `efectivo arriesgado / (StopLossPips * tamaño del pip)`. Si `StopLossPips` es cero o el valor de la cartera no está disponible, la estrategia se niega a operar.

## Protección y trailing

- Los niveles de stop-loss y take-profit se rastrean internamente y se comparan con los máximos/mínimos de las velas, emulando las órdenes protectoras de MetaTrader.
- El trailing stop se activa solo después de que la ganancia no realizada supere `TrailingStopPips + TrailingStepPips` pips, cumpliendo el requisito original de que tanto un offset inicial como un paso mínimo deben satisfacerse antes de mover el stop.

## Alineación multitemporal

Todos los indicadores se recalculan en cada vela cerrada de su respectivo marco temporal. El marco temporal lento espera a que las tres medias móviles y los stochastics formen y utiliza los valores más recientes de la media móvil desplazada, imitando el parámetro de desplazamiento `iMA` de MetaTrader. Esto asegura que el port de StockSharp dispara trades en la misma barra que el experto MQL original.
