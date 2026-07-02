# Utilidad para cerrar la cesta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
The Basket Close Utility strategy mirrors the behaviour of the MetaTrader expert "Basket Close 2". It continuously monitors the floating profit and loss of every open position in the connected portfolio. When either a configurable profit objective or a loss limit is reached, the strategy sends market orders to flatten **all** exposures in every instrument involved. Opcionalmente, puede abrir automáticamente una pequeña posición de prueba siempre que el libro esté plano, lo cual es útil dentro de las pruebas retrospectivas para validar que la lógica de protección funciona como se espera.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `LossMode` | Elige si el protector contra pérdidas compara porcentajes o valores de moneda. |
| `LossPercentage` | Negative percentage drawdown (expressed in absolute value) that triggers the loss exit when `LossMode` is `Percentage`. |
| `LossCurrency` | Pérdida flotante en la moneda de la cuenta que desencadena la salida cuando `LossMode` es `Currency`. |
| `ProfitMode` | Elige si el objetivo de ganancias compara porcentajes o valores de moneda. |
| `ProfitPercentage` | Percentage gain that closes all positions when `ProfitMode` is `Percentage`. |
| `ProfitCurrency` | Beneficio flotante en la moneda de la cuenta que cierra todas las posiciones cuando `ProfitMode` es `Currency`. |
| `CandleType` | Timeframe used to trigger periodic checks of the floating profit and loss. |
| `EnableTestOrders` | Cuando está habilitada, la estrategia envía una orden de compra de mercado única siempre que no haya posiciones abiertas. |
| `TestOrderVolume` | Tamaño de la operación utilizado cuando la orden de prueba opcional está activa. |

## Lógica de trading
1. Subscribe to the configured candle series and run the evaluation only when a candle is fully finished, matching the behaviour of the original EA that works on closed bars.
2. Aggregate the floating profit and loss of every open position. Si el objeto de la cartera expone un beneficio flotante combinado, se utiliza; de lo contrario, la estrategia suma el PnL de cada posición.
3. Compute the percentage change relative to the current account balance captured at start-up.
4. Trigger the loss routine when the floating PnL breaches the configured limit. Trigger the profit routine when the floating PnL or the percentage gain reaches the profit target.
5. Una vez activado, siga enviando órdenes de mercado hasta que se cierren todas las posiciones abiertas en toda la cartera. Esto incluye la seguridad principal, así como las posiciones abiertas por las estrategias infantiles.
6. Opcionalmente, envíe una orden de mercado para reabrir la exposición (para realizar pruebas) después de que el libro se estabilice.

## Notas
- El experto MetaTrader mostró información textual en el gráfico. En StockSharp las cifras importantes se registran a través de `LogInfo`.
- Los ajustes de swaps y comisiones del guión original se incluyen implícitamente dentro del PnL flotante informado por la cartera o las posiciones individuales.
- The percentage thresholds use the account balance captured when the strategy starts. Ajuste los límites cuando realice sesiones largas si la base de capital cambia sustancialmente.
- Cuando la orden de prueba opcional está habilitada, la orden auxiliar se vuelve a emitir siempre que la guardia de pérdidas o ganancias haya cerrado la exposición anterior.
