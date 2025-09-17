# Three Typical Candles Strategy

## Overview
The **Three Typical Candles Strategy** recreates the MetaTrader Expert Advisor "Three Typical Candles" inside the StockSharp high-level API. The system observes the typical price of the last three completed candles and trades when it detects a strictly monotonic sequence. Typical price is defined as the arithmetic mean of the high, low, and close of a candle. When the three most recent finished candles form a rising sequence of typical prices, the strategy enters long. Conversely, a falling sequence triggers a short entry.

The port closely follows the original MQL5 logic:
- Signals are evaluated only once per finished candle to avoid intrabar noise.
- A configurable trading window can disable trading outside selected hours and forces the strategy flat when the filter is active.
- Opposite positions are closed before a new one is opened so the strategy never holds both directions at the same time.
- Order volume mirrors the source EA by using a fixed lot size while respecting the exchange volume step, as well as minimum and maximum volume constraints reported by the security.

## Trading Rules
1. **Signal detection**
   - Compute typical price `Tp = (High + Low + Close) / 3` for each finished candle.
   - Track the two previous typical values. Once three values are available, check for a strictly rising or strictly falling sequence.
2. **Long entry**
   - If `Tp[-2] < Tp[-1] < Tp[0]` (three rising typical prices) and the current position is not long, the strategy closes any short exposure and sends a market buy order.
3. **Short entry**
   - If `Tp[-2] > Tp[-1] > Tp[0]` (three falling typical prices) and the current position is not short, the strategy closes any long exposure and sends a market sell order.
4. **Time control**
   - When the optional time filter is enabled, the strategy evaluates the signal only when the candle open time falls within the configured trading session. Outside that window, any open position is liquidated immediately and no new trades are placed.
5. **Position management**
   - The strategy has no explicit stop-loss or take-profit levels. Risk management should be handled externally (e.g., via protective strategies or manual supervision).

## Parameters
| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Volume` | decimal | `1` | Fixed order volume (lots or contracts). The strategy automatically rounds the value to the nearest valid volume step and enforces minimum/maximum limits of the instrument. |
| `UseTimeControl` | bool | `true` | Enables the intraday trading window filter. When disabled, signals are evaluated around the clock. |
| `StartHour` | int | `11` | Inclusive start hour (0-23) of the trading window when `UseTimeControl` is true. |
| `EndHour` | int | `17` | Exclusive end hour (0-23) of the trading window when `UseTimeControl` is true. If the end hour is less than the start hour, the window spans midnight. |
| `CandleType` | `DataType` | `TimeFrame(1h)` | Candle type used for analysis. Select a timeframe compatible with your data feed. |

## Implementation Notes
- The StockSharp `Strategy` base class handles subscriptions and order routing. Signals are evaluated in `ProcessCandle`, which receives completed candles via the high-level binding API.
- Market orders are issued through `BuyMarket` and `SellMarket`. When a reversal occurs, the strategy first closes the existing exposure using an opposite market order before sending the new entry.
- `StartProtection()` is called during initialization to allow attaching optional protective mechanisms if desired.
- The `GetTradeVolume` helper mirrors MetaTrader's lot normalization by adjusting the configured volume to exchange constraints (volume step, minimum, and maximum volume).
- The strategy stores only two historical typical prices, which is sufficient to evaluate the three-candle pattern without maintaining large collections.

## Usage Tips
- Attach the strategy to an instrument with sufficient liquidity. The original EA used intraday Forex data, but any market that provides OHLC candles can be used.
- Choose a candle timeframe that fits your trading horizon. The default one-hour candles replicate the behaviour of the source EA, yet shorter or longer intervals can be explored through parameter optimization.
- Consider pairing the strategy with risk controls such as maximum drawdown limits or portfolio-level stop loss via the StockSharp protective strategies framework.
- Backtest across multiple instruments and trading sessions to confirm that the strictly monotonic pattern produces actionable signals under your market conditions.
