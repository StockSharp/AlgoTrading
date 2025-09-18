# Risk Management ATR Strategy

## Overview
The Risk Management ATR strategy is a StockSharp conversion of the MetaTrader 5 expert *Risk Management EA Based on ATR Volatility*. The original EA focussed on automatically sizing positions according to the account balance and the current market volatility measured by the Average True Range (ATR). The StockSharp port keeps the same philosophy: it only opens long positions when a 10-period simple moving average crosses above a 20-period simple moving average, and every entry size is computed so that the potential loss at the protective stop matches the configured risk percentage.

The conversion follows the high-level StockSharp API. Indicator calculations rely on `AverageTrueRange` and `SimpleMovingAverage` components attached to the candle subscription instead of direct indicator calls. Trade management reuses StockSharp helper methods, cancelling and recreating the protective stop after each fill so the net position and the stop order always match.

## Trading logic
1. Subscribe to the timeframe defined by `CandleType` and wait for fully closed candles to avoid premature decisions.
2. Feed a 14-period ATR and two simple moving averages (lengths 10 and 20) with the subscription data.
3. When the fast moving average closes above the slow moving average and there is no open position, calculate the position size based on the selected risk model and submit a market buy order.
4. After each fill, compute the stop-loss distance: either `ATR * AtrMultiplier` or a fixed number of price steps when `UseAtrStopLoss` is disabled.
5. Round the stop price down to the nearest tick and place a `SellStop` order with the current position size. Any previous stop is cancelled before the new one is registered.
6. When the stop order executes and the position returns to zero the strategy clears its internal state, ready for the next crossover.

## Risk management
- `RiskPercentage` determines how much of the portfolio value can be lost on a single trade. The strategy reads `Portfolio.CurrentValue` (or `BeginValue` as a fallback) and multiplies it by the percentage to obtain the allowed monetary risk.
- The allowed risk is divided by the stop-loss distance to obtain the trade volume. Volume rounding honours the instrument volume step, minimum and maximum constraints so the generated orders remain valid on the exchange.
- If `RiskPercentage` is set to `0`, the strategy falls back to the default `Volume` property (1 lot by default) while keeping the automatic protective stop.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute timeframe | Primary candle series processed by the strategy. |
| `AtrPeriod` | `int` | `14` | Number of candles used to smooth the ATR indicator. |
| `AtrMultiplier` | `decimal` | `2.0` | Multiplier applied to the ATR value to derive the stop-loss distance. |
| `RiskPercentage` | `decimal` | `1.0` | Percentage of the portfolio value risked on each trade. Set to zero to use a fixed volume. |
| `UseAtrStopLoss` | `bool` | `true` | When enabled the stop is placed at `ATR * AtrMultiplier`; otherwise a fixed distance is used. |
| `FixedStopLossPoints` | `int` | `50` | Number of price steps used for the protective stop whenever ATR-based placement is disabled. |

## Differences from the original EA
- StockSharp works with net positions, therefore the conversion only submits market buy orders. Exits happen through the protective `SellStop`, which reproduces the EA behaviour of always being flat after a stop.
- MetaTrader exposes the `_Point` constant for tick size. The port queries `Security.PriceStep` and falls back to a single currency unit when the instrument does not provide a tick specification.
- Position sizing respects StockSharp’s volume filters (`VolumeStep`, `MinVolume`, `MaxVolume`) to ensure the order book accepts the generated order sizes.
- Indicator processing is event-driven through `Subscription.Bind(...)` instead of synchronous `iMA`/`iATR` calls.

## Usage tips
- Make sure the connected portfolio reports a correct `CurrentValue`; otherwise the risk-based position sizing will fall back to zero volume.
- The `Volume` property still acts as a safety net. If you want a fixed lot size regardless of ATR calculations, set `RiskPercentage` to zero and adjust `Volume` before starting the strategy.
- Attach the strategy to a chart to visualise the candles, both moving averages and executed trades. It helps confirm that new entries only appear when the fast average closes above the slow one and that stops sit exactly below the latest price swing.
- Consider increasing `AtrMultiplier` on more volatile instruments to avoid premature stop-outs, or disable ATR-based placement and provide a custom fixed distance through `FixedStopLossPoints`.

## Indicators
- `AverageTrueRange` (length `AtrPeriod`).
- `SimpleMovingAverage` (fast length `10`).
- `SimpleMovingAverage` (slow length `20`).
