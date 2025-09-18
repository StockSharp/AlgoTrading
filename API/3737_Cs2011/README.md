# Cs2011 Strategy

## Overview
The Cs2011 strategy is a reversal system translated from the original `cs2011.mq5` expert advisor. It monitors the MACD histogram and signal line on every finished candle and searches for exhaustion patterns around the zero line. The C# port keeps the core timing rules while exposing them through the high level StockSharp API.

## Trading logic
- **Zero line reversals** – when the MACD value from the previous bar is above zero while the bar before that was below zero, the strategy issues a **short** signal. The opposite transition (from positive to negative) issues a **long** signal. This mimics the contrarian entries implemented in the MQL5 script.
- **Signal line extremes** – the strategy stores the last three signal-line readings. A local maximum while MACD stayed negative triggers an additional short entry; a local minimum while MACD stayed positive triggers a long entry. This reproduces the pattern checks based on `Sig[0]`, `Sig[1]` and `Sig[2]` in the source EA.
- Signals are evaluated only on finished candles supplied by `SubscribeCandles`, so partial data is ignored.

## Position handling
- The strategy targets a **fixed absolute position size** (`TargetVolume`). When a bullish signal arrives it buys enough contracts to reach `+TargetVolume`. Bearish signals do the same for `-TargetVolume`. Existing exposure in the same direction is respected – no additional orders are placed if the target is already reached.
- `StartProtection` mirrors the original take-profit and stop-loss settings. Point distances are converted into `UnitTypes.Point` values and passed to the built-in risk module. Leaving either value at `0` disables the corresponding barrier.
- High level helpers (`BuyMarket`, `SellMarket`) are used instead of the low level request structure from the MQL version.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TargetVolume` | `1` lot | Absolute position size achieved after a signal. Replaces the `Risk` × balance sizing routine from the EA. |
| `TakeProfitPoints` | `2200` | Distance in price points for take-profit management. `0` disables the take-profit. |
| `StopLossPoints` | `0` | Distance in price points for the stop-loss. `0` disables the stop-loss, matching the EA defaults. |
| `FastEmaPeriod` | `30` | Fast EMA length for the MACD core. |
| `SlowEmaPeriod` | `500` | Slow EMA length for MACD. |
| `SignalPeriod` | `36` | Signal line smoothing period. |
| `CandleType` | `1 hour` time frame | Candle source used by `SubscribeCandles`. Adjust this to match the chart period used in MetaTrader. |

All parameters are registered through `Param()` so they can be optimized inside the StockSharp optimizer UI.

## Differences from the MQL5 version
- The money-management routine (`Money_M`) relied on historical deals and the MetaTrader account balance. StockSharp strategies operate on broker-agnostic portfolios, therefore the port exposes a simple `TargetVolume` parameter. Users can connect their own money management by overriding the parameter value or the `ExecuteSignals` method.
- Order requests are simplified to single market orders. Re-try logic, spread-based deviation and trade context checks are handled by StockSharp infrastructure.
- The strategy runs on candle subscriptions instead of the custom `IsNewBar` helper. This guarantees that only fully formed candles are processed.

## Usage notes
1. Configure the security, portfolio and candle type before launching the strategy.
2. Tune `TargetVolume` to match the desired nominal lot size.
3. Optionally adjust `TakeProfitPoints` and `StopLossPoints` to reproduce the protective levels from the original EA.
4. Start the strategy – logging messages record every trade trigger along with the targeted exposure.

The code contains English inline comments describing each step of the porting process.
