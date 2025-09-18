# Nina EA Strategy

## Overview
The Nina EA strategy is a one-position trend follower converted from the MetaTrader 4 expert "NinaEA". The original robot uses a custom indicator named **NINA** and trades whenever the difference between the indicator's bullish and bearish buffers crosses above or below zero. In the StockSharp version the custom indicator is replaced with the built-in **SuperTrend** indicator, which also publishes separate bullish and bearish buffers. A flip in SuperTrend direction serves as the zero-crossing proxy: when the trend turns bullish the strategy buys, and when it turns bearish it sells.

The strategy always keeps at most one open position. An opposite signal immediately closes the existing position and establishes a new trade in the new direction. An optional stop-loss expressed in price points can be enabled to mimic the original "StopLoss" input.

## Trading Logic
1. Subscribe to the configured candle series and calculate SuperTrend with the supplied ATR period and multiplier.
2. Wait until both the strategy and the indicator are formed before reacting to signals.
3. On every completed candle:
   - If a protective stop price is touched, exit the open position at market.
   - If SuperTrend flips from bearish to bullish, close any short exposure and buy with the configured volume.
   - If SuperTrend flips from bullish to bearish, close any long exposure and sell with the configured volume.
   - Store the current SuperTrend direction to detect the next flip.

The logic replicates the MetaTrader expert's behavior, where `nina = Buffer0 - Buffer1` and a sign change drives both exits and new entries.

## Position and Risk Management
- Only a single position can be active at a time; all trades reverse the direction rather than stacking multiple orders.
- An optional stop-loss in price points is calculated from the fill price. For a long trade the stop is placed below the entry, and for a short trade it is placed above the entry. Setting the parameter to zero disables the stop.
- `StartProtection()` is called so that built-in StockSharp protections can be configured if desired.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `Volume` | `0.1` | Order volume used for every new entry. |
| `AtrPeriod` | `10` | ATR period passed to the SuperTrend calculation (maps the original `PeriodWATR`). |
| `AtrMultiplier` | `1` | ATR multiplier for SuperTrend (maps the original `Kwatr`). |
| `StopLossPoints` | `0` | Optional stop-loss distance in price points. Zero keeps the stop disabled, identical to the MetaTrader code that sent market orders without a stop price. |
| `CandleType` | `TimeFrame(1 minute)` | Candle series that feeds the indicator and trading logic. |

## Conversion Notes
- The MetaTrader expert relied on the custom `NINA` indicator. Its two buffers were interpreted as bullish/bearish SuperTrend lines because only their difference and sign mattered for trading. SuperTrend exposes the same information through its `IsUpTrend` flag, which makes it a suitable high-level replacement requiring no manual buffer handling.
- Order closing logic mirrors the `OrdersTotal()` loop from the original script: a trend flip first flatters the current position and then opens a trade in the new direction.
- The unused MetaTrader inputs (`highlow`, `cbars`, `from`, `maP`, `SMAspread`, `Slippage`) are omitted because they do not influence the trading rules in the original file.

## Usage Tips
1. Attach the strategy to a security and configure the candle timeframe that matches your MetaTrader test.
2. Tune the ATR period and multiplier to replicate the behaviour of the original indicator.
3. Increase `StopLossPoints` if you want a hard risk limit; otherwise leave it at zero for pure signal-based exits.
