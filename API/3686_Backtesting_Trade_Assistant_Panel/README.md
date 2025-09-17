# Backtesting Trade Assistant Panel Strategy

## Overview
The **Backtesting Trade Assistant Panel Strategy** is a manual helper converted from the MetaTrader 4 expert advisor *Backtesting Trade Assistant Panel V1.10*. The original script created a graphical control panel inside the tester that let the operator change lot size, stop-loss and take-profit distances, and instantly submit BUY or SELL market orders. The StockSharp port offers the same workflow inside a strategy component by exposing strongly typed parameters and public helper methods instead of on-chart widgets.

Key capabilities:

- Maintain configurable order volume together with MetaTrader-style stop-loss and take-profit distances (measured in “points”).
- Issue long or short market orders on demand through the `ManualBuy()` and `ManualSell()` helpers.
- Automatically attach stop-loss and take-profit offsets after each manual entry using the converted point values.
- Provide utility methods that update the trading volume and risk distances at runtime, mimicking the editable text fields of the MT4 panel.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Volume in lots applied to manual market orders. Changing the value also updates the base `Strategy.Volume`. | `0.1` |
| `StopLossPips` | Distance from the fill price to the protective stop, expressed in MetaTrader points. Set to `0` to disable automatic stop-loss placement. | `50` |
| `TakeProfitPips` | Distance from the fill price to the profit target, expressed in MetaTrader points. Set to `0` to disable automatic take-profit placement. | `100` |
| `MagicNumber` | Identifier preserved from the original EA for bookkeeping. It is not used directly by StockSharp execution logic but can be referenced in custom extensions. | `99` |

## Manual operations
The original EA relied on clickable buttons. In StockSharp the same actions are available as public methods:

- `SetOrderVolume(decimal volume)` – synchronizes the `OrderVolume` parameter and the internal `Strategy.Volume` value.
- `SetStopLoss(decimal pips)` / `SetTakeProfit(decimal pips)` – adjust the protective distances while the strategy is running. Values are interpreted in MetaTrader points, exactly like the MT4 text boxes.
- `ManualBuy()` – submits a market buy order using the current volume. After execution the strategy converts the configured stop-loss and take-profit points into price offsets (using symbol metadata) and calls `SetStopLoss`/`SetTakeProfit` to register protective orders for the resulting net position.
- `ManualSell()` – symmetric helper for market sell orders.
- `CloseAllPositions()` – closes the entire exposure at market price. This mirrors the workflow where the tester could flatten positions manually.

All protective distances are converted with the same pip-size heuristic as in MT4: five- and three-digit symbols multiply `PriceStep` by ten to obtain a single “point”, while other symbols rely on the raw `PriceStep`. If market data does not provide the necessary metadata, a fallback size of `0.0001` is used to preserve consistent behaviour.

## Behavioural notes
- The strategy subscribes to Level1 updates to keep track of the best bid/ask. When those prices are unavailable it falls back to the last trade price before attaching protective offsets.
- No automatic trading signals are generated—this module acts strictly as a manual execution assistant just like the MT4 panel.
- Because StockSharp manages protective orders natively, there is no need for an explicit magic number. The field is included purely for parity with the source expert advisor.
- Stop-loss and take-profit distances can be adjusted at any time before triggering `ManualBuy()`/`ManualSell()` to emulate editing the MT4 text fields prior to pressing the buttons.

## Differences from the original EA
- The MetaTrader user interface is replaced by strategy parameters and method calls. All functionality is available programmatically without rendering chart controls.
- Slippage handling from the MT4 `OrderSend` call (fixed at 50 points) is not reproduced because StockSharp’s `BuyMarket`/`SellMarket` helpers do not expose a direct slippage argument. The surrounding environment should manage execution tolerance if required.
- Protective orders are created with StockSharp’s high-level `SetStopLoss`/`SetTakeProfit` helpers instead of direct `OrderSend` calls, keeping the implementation consistent with StockSharp conventions.

## Usage tips
1. Configure the desired symbol, portfolio, and connector in StockSharp as usual, then start the strategy.
2. Adjust `OrderVolume`, `StopLossPips`, and `TakeProfitPips` through the parameter grid or the provided setter methods.
3. Call `ManualBuy()` or `ManualSell()` whenever a discretionary entry is needed. The helper will attach the requested protective orders automatically.
4. Use `CloseAllPositions()` to flatten the exposure instantly during backtests or live discretionary trading sessions.
