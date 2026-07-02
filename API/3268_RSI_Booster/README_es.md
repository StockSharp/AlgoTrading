# Estrategia RsiBoosterStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`RsiBoosterStrategy` es una adaptación a StockSharp del asesor experto de MetaTrader *RSI booster*. La estrategia compara el valor rápido de RSI calculado sobre la vela actual con un RSI retrasado que usa la vela anterior. Cuando la diferencia supera una relación definida por el usuario, la estrategia abre una posición de mercado y después gestiona la operación con stops fijos, objetivos de take-profit, un trailing stop opcional y una cadena de órdenes inversas para recuperación de pérdidas.

La estrategia está construida sobre la API de alto nivel de StockSharp. Se suscribe a una sola serie de velas, utiliza los indicadores incorporados `RelativeStrengthIndex` y emplea el sistema de parámetros de estrategia para que todas las entradas estén disponibles para optimización dentro de Designer.

## Lógica de trading

1. Se calculan dos indicadores RSI en cada vela cerrada.
   * El RSI rápido usa `FirstRsiPeriod` y `FirstRsiPrice`, y lee la vela más reciente.
   * El RSI retrasado usa `SecondRsiPeriod` y `SecondRsiPrice`, pero la estrategia conserva el valor anterior para que actúe con un retraso de una barra.
2. Cuando `fast RSI - delayed RSI` es mayor que `Ratio`, la estrategia compra si no hay una posición larga abierta. Cuando la diferencia está por debajo de `-Ratio`, vende si no hay una posición corta abierta.
3. `OnlyOnePositionPerBar` garantiza que como máximo se produzca una entrada por dirección para la misma marca temporal de vela.
4. Después de cada vela, la estrategia evalúa las reglas de stop-loss, take-profit y trailing. Si se activa alguna condición, la posición se cierra de inmediato.
5. Cuando una posición se cierra con PnL realizado negativo, la lógica opcional de recuperación puede abrir una posición inversa (dirección opuesta) con el mismo volumen. El número de operaciones de recuperación encadenadas está limitado por `ReturnOrdersMax`.

## Gestión de riesgos

* **Stop-loss** - expresado en puntos del instrumento mediante `StopLossPips`. La posición se cierra cuando el precio cruza el nivel de stop.
* **Take-profit** - expresado en puntos del instrumento mediante `TakeProfitPips`.
* **Trailing stop** - si está habilitado mediante `TrailingStopPips`, el stop empieza a seguir al precio cuando la ganancia supera la distancia configurada. `TrailingStepPips` define la mejora mínima necesaria antes de mover el nivel trailing.
* **Orden de retorno** - se activa cuando `ReturnOrderEnabled` es `true`. Después de una operación perdedora, la estrategia abre al instante una orden de mercado en la dirección opuesta mientras cuenta cuántas órdenes de recuperación se han emitido.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen de negociación usado para cada orden de mercado (lotes o contratos). |
| `Ratio` | Diferencia mínima de RSI necesaria para abrir una posición. |
| `StopLossPips` | Distancia del stop-loss en puntos del instrumento. |
| `TakeProfitPips` | Distancia del take-profit en puntos del instrumento. |
| `TrailingStopPips` | Distancia del trailing stop en puntos del instrumento. |
| `TrailingStepPips` | Mejora mínima antes de mover el trailing stop. |
| `OnlyOnePositionPerBar` | Impide múltiples entradas durante la misma vela. |
| `ReturnOrderEnabled` | Habilita la lógica de recuperación con orden inversa. |
| `ReturnOrdersMax` | Número máximo de órdenes de recuperación consecutivas. |
| `FirstRsiPeriod` | Período del RSI rápido. |
| `FirstRsiPrice` | Fuente de precio para el RSI rápido (coincide con los modos de precio aplicado de MetaTrader). |
| `SecondRsiPeriod` | Período del RSI retrasado. |
| `SecondRsiPrice` | Fuente de precio para el RSI retrasado (coincide con los modos de precio aplicado de MetaTrader). |
| `CandleType` | Serie de velas usada para el análisis. |

## Notas

* La conversión del paso de precio respeta el `PriceStep` del instrumento siempre que esté disponible. Si el instrumento no proporciona un paso de precio, se usa un valor de reserva de `0.0001`.
* El contador de la cadena de recuperación se reinicia cuando se produce una operación rentable o cuando se alcanza el número máximo configurado de órdenes de recuperación.
* La estrategia dibuja ambos indicadores RSI en el área del gráfico para una inspección visual rápida junto con las operaciones ejecutadas.
