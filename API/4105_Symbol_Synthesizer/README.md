# Symbol Synthesizer Strategy

## Overview

The **Symbol Synthesizer Strategy** is a C# conversion of the MetaTrader 5 expert *SymbolSynthesizer.mq5*. The original MQL5 script displayed a manual trading panel that calculated synthetic FX crosses from two underlying legs and placed paired hedge orders on request. This StockSharp port preserves the core behaviour:

* Subscribe to two leg securities and continuously recompute the synthetic cross bid/ask.
* Present the same thirteen predefined currency combinations from the original expert.
* Execute two coordinated orders when the operator requests a synthetic **Buy** or **Sell** action.
* Adjust the second leg volume using the MetaTrader tick value and point ratio formula so that the hedge keeps its original notional exposure.

The strategy is intentionally manual. It waits for price updates, logs the synthetic quotes, and reacts only when the `TradeAction` parameter is set to `Buy` or `Sell`, mimicking the buttons from the MetaTrader panel.

## Predefined combinations

The table below reproduces the MQL5 `Sym` array. Index `0` matches the chart symbol in the original script and is also the default value for the `CombinationIndex` parameter.

| Index | Synthetic symbol | First leg | Second leg | Composition |
|-------|------------------|-----------|------------|-------------|
| 0 | EURUSD | EURGBP | GBPUSD | Product (legs traded in the same direction) |
| 1 | GBPUSD | EURGBP | EURUSD | Ratio (first leg traded opposite to the second) |
| 2 | USDCHF | EURUSD | EURCHF | Ratio |
| 3 | USDJPY | EURUSD | EURJPY | Ratio |
| 4 | USDCAD | EURUSD | EURCAD | Ratio |
| 5 | AUDUSD | EURAUD | EURUSD | Ratio |
| 6 | EURGBP | GBPUSD | EURUSD | Ratio |
| 7 | EURAUD | AUDUSD | EURUSD | Ratio |
| 8 | EURCHF | EURUSD | USDCHF | Product |
| 9 | EURJPY | EURUSD | USDJPY | Product |
| 10 | GBPJPY | GBPUSD | USDJPY | Product |
| 11 | AUDJPY | AUDUSD | USDJPY | Product |
| 12 | GBPCHF | GBPUSD | USDCHF | Product |

For product combinations both legs are traded in the same direction. For ratio combinations the first leg is traded opposite to the second leg.

## Parameters

| Name | Description |
|------|-------------|
| `CombinationIndex` | Index of the predefined combination (0–12). The combination is resolved on start; change requires a restart. |
| `OrderVolume` | Initial volume for the first leg. The strategy normalises it using the leg volume step and enforces the minimum volume. |
| `Slippage` | Maximum slippage in price steps. Limit orders are shifted by `Slippage × PriceStep` away from the reference bid/ask to mimic the MetaTrader deviation parameter. |
| `TradeAction` | Manual trigger (`None`, `Buy`, `Sell`). Set to `Buy` or `Sell` to replicate the original panel button; the strategy resets the value to `None` after execution or after logging an error. |

## Data subscriptions

The strategy subscribes to Level1 data (best bid/ask) for both leg securities selected by the `CombinationIndex`. When enough quotes are available it calculates the synthetic bid/ask:

* Product: `vBid = bid1 × bid2`, `vAsk = ask1 × ask2`
* Ratio: `vBid = bid2 / bid1`, `vAsk = ask2 / ask1`

Every change is logged to provide the operator with up-to-date virtual pricing.

## Order placement logic

1. The first leg order volume equals the `OrderVolume` parameter after normalisation.
2. The second leg volume reuses the MetaTrader formula:
   
   `vol2 = vol1 × syntheticPrice ÷ tickValue1 ÷ tickValue2 × (point2 ÷ point1)`
   
   where `tickValue` corresponds to `Security.StepPrice` and `point` corresponds to `Security.PriceStep`.
3. Order directions mirror the MQL5 logic:
   * **Product combinations:** first leg follows the requested direction, second leg always follows the requested direction.
   * **Ratio combinations:** first leg trades opposite to the requested direction, second leg follows the requested direction.
4. Prices are derived from the latest bid/ask of each leg. Limit orders include the configured slippage offset and are normalised with `Security.ShrinkPrice`.

If any required metadata (price step, tick value, volume step) is missing the strategy logs an error and skips the order, matching the failure behaviour of the original expert.

## Usage notes

* Set the main `Security` and `Portfolio` before starting. The strategy automatically resolves additional leg securities using the StockSharp symbol lookup.
* Ensure the data provider supplies valid `PriceStep`, `StepPrice`, and `VolumeStep` values, otherwise the hedge volume calculation cannot be performed.
* The strategy is manual by design; no automatic trade logic is added beyond the button emulation.
* Restart the strategy if you need to switch to a different combination index.
