# Martingale Estrategia inteligente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Martingale Smart es una conversión del MetaTrader asesor experto "Martingale Smart". La estrategia mantiene sólo una posición abierta a la vez y cambia entre dos filtros de entrada diferentes después de cada ciclo perdedor:

1. **Filtro principal**: cruce entre dos promedios móviles simples combinados con la dirección de un histograma de marco temporal más alto MACD. Este es el modo de entrada predeterminado.
2. **Filtro secundario** – envolventes de media móvil. Cuando la pérdida flotante del ciclo anterior es negativa, la estrategia cambia a este filtro. Otra pérdida vuelve al filtro primario.

El componente martingala aumenta el volumen de la siguiente operación después de un ciclo perdedor. Puedes multiplicar el último volumen (martingala clásica) o agregar un incremento fijo.

## Suscripciones de datos

* `CandleType` – período de tiempo utilizado para los cálculos principales y la gestión comercial.
* `MacdTimeFrame`: período de tiempo secundario dedicado al filtro MACD. El valor predeterminado es un mes para imitar el EA original que utilizó el período de tiempo `PERIOD_MN1`.

Ambas suscripciones se inician automáticamente en `OnStarted`.

## Lógica comercial

1. Se considera una nueva operación sólo si no hay ninguna posición abierta y todos los indicadores están formados.
2. El filtro primario se pone largo cuando la MA rápida está por debajo de la MA lenta y la línea MACD está por encima de la señal (la misma lógica para casos bajistas). Estas condiciones siguen el EA original que usaba `iMA` y `iMACD` con un cambio de una barra.
3. El filtro secundario utiliza una envolvente de media móvil simple. Un cierre por encima de la banda inferior indica una entrada larga, mientras que un cierre por debajo de la banda superior indica una entrada corta. Esto reproduce la lógica basada en `iEnvelopes`.
4. Cuando un ciclo termina con un beneficio negativo, la estrategia cambia al filtro alternativo y calcula el siguiente volumen de acuerdo con los parámetros de martingala. Un ciclo rentable mantiene el filtro actual y restablece el volumen al valor inicial.
5. Los niveles protectores de stop-loss y take-profit se adjuntan inmediatamente después de cada entrada utilizando distancias basadas en pips.

## Gestión de riesgos

* **Tope de equilibrio**: una vez que la ganancia no realizada alcanza `BreakEvenTriggerPips`, el límite de pérdidas salta al precio de entrada más una compensación opcional.
* **Stop dinámico clásico**: mantiene un stop móvil que se mantiene a `TrailingStopPips` de distancia del último cierre.
* **Obtener ganancias en dinero** – cierra la posición cuando la ganancia flotante excede `MoneyTakeProfit`.
* **Obtener ganancias en porcentaje**: similar al objetivo monetario, pero expresado como porcentaje del valor actual de la cartera.
* **Money trailing stop**: se activa cuando la ganancia flotante alcanza `MoneyTrailingTarget`; posteriormente, la estrategia realiza un seguimiento del pico de ganancias y liquida la posición cuando la reducción excede `MoneyTrailingDrawdown`.

Todos los cálculos monetarios se basan en los `PriceStep` y `StepPrice` del instrumento. Si la fuente de datos no los proporciona, la estrategia recurre a una simple estimación de precio × volumen.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `UseMoneyTakeProfit` | Habilite la regla de toma de ganancias monetaria fija. |
| `MoneyTakeProfit` | Objetivo de beneficio flotante en la moneda de la cuenta. |
| `UsePercentTakeProfit` | Habilite la toma de ganancias basada en porcentaje. |
| `PercentTakeProfit` | Objetivo de beneficio flotante como % del valor de la cartera. |
| `EnableMoneyTrailing` | Habilite el seguimiento de las ganancias en dinero. |
| `MoneyTrailingTarget` | Nivel de beneficio que habilita el trailing block. |
| `MoneyTrailingDrawdown` | Máxima devolución de ganancias permitida una vez que el seguimiento esté activo. |
| `UseBreakEven` | Mueva el stop-loss al punto de equilibrio después de la distancia configurada. |
| `BreakEvenTriggerPips` | Distancia de beneficio en pips requerida antes de que se mueva el stop. |
| `BreakEvenOffsetPips` | Se agregaron pips adicionales al punto de equilibrio. |
| `MartingaleMultiplier` | Factor de multiplicación aplicado después de un ciclo perdedor. |
| `InitialVolume` | Volumen utilizado para el primer pedido de cada ciclo. |
| `UseDoubleVolume` | Si es cierto, multiplique el volumen; de lo contrario, aplique `LotIncrement`. |
| `LotIncrement` | Incremento de lote fijo utilizado cuando la duplicación está deshabilitada. |
| `TrailingStopPips` | Distancia del trailing stop clásico en pips. |
| `StopLossPips` | Distancia inicial de stop-loss en pips. |
| `TakeProfitPips` | Distancia inicial de toma de ganancias en pips. |
| `FastMaPeriod` | Período de la media móvil rápida. |
| `SlowMaPeriod` | Período de la media móvil lenta. |
| `EnvelopePeriod` | Período de la media móvil envolvente. |
| `EnvelopeDeviation` | Ancho del sobre en porcentaje. |
| `MacdFastLength` | Longitud rápida de EMA dentro del MACD. |
| `MacdSlowLength` | Longitud lenta de EMA dentro del MACD. |
| `MacdSignalLength` | Longitud de la señal EMA dentro del MACD. |
| `CandleType` | Periodo de tiempo de la señal principal. |
| `MacdTimeFrame` | Plazo de las velas MACD. |

## Notas de uso

1. El paso de martingala se ejecuta sólo cuando la posición anterior se cerró completamente con pérdida.
2. La estrategia espera una posición abierta a la vez; siempre liquida la posición actual antes de entrar en la dirección opuesta.
3. Para obtener umbrales precisos basados en dinero, configure las especificaciones del contrato del instrumento (`PriceStep`, `StepPrice` y `VolumeStep`).
4. El punto de equilibrio y los trailingstops se evalúan sobre velas cerradas en el marco temporal principal; Los picos intrabar se ignoran.

## Diferencias vs. el MetaTrader EA

* La conversión utiliza el API de alto nivel de StockSharp (`SubscribeCandles` + `Bind`) y el indicador `MovingAverageConvergenceDivergenceSignal` en lugar de llamadas directas a `iMACD`.
* Algunas comprobaciones específicas del corredor (niveles de congelación, llamadas manuales de correo/notificación, bucles basados en tickets) se omiten porque el motor StockSharp gestiona esos aspectos internamente.
* Las protecciones basadas en dinero operan en posiciones agregadas en lugar de cálculos por ticket, alineándose con el modelo de cuenta de StockSharp.
