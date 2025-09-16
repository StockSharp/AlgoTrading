# Slow Stochastic Mode Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Slow Stochastic Mode Strategy** is a conversion of the MetaTrader expert advisor `Exp_Slow-Stoch.mq5` to the StockSharp high-level API. The system trades on the closing price of finished candles and uses a smoothed stochastic oscillator to detect regime changes. Three distinct signal modes are available so the trader can decide whether to react to level breaks, momentum twists, or line crossings.

## Core Idea

The strategy observes the %K and %D lines of a slow stochastic oscillator that is additionally smoothed by the `Slowing` parameter. Depending on the selected *Signal Mode*, the algorithm evaluates the oscillator one or more bars back (controlled by `SignalBar`) and either opens a new position or closes the opposite side when a qualifying event appears. Orders are always placed with market executions.

## Signal Modes

- **Breakdown** – looks for %K breaking through the 50 level. A cross from below to above 50 generates a long entry and closes short positions. A cross from above to below 50 produces a short entry and closes long positions.
- **Twist** – detects a direction change of %K. When the oscillator had been falling two bars ago and turns upward on the evaluated bar, the strategy opens or reverses into a long trade. The inverse situation triggers shorts.
- **CloudTwist** – tracks the colour change of the stochastic "cloud" by watching a %K vs %D cross. A bullish cross (%K above %D) opens or protects longs, while a bearish cross (%K below %D) does the opposite.

All modes respect the four permission toggles: long/short entries and long/short exits can be independently enabled or disabled.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | H4 time frame | Candle type used for indicator calculations. |
| `KPeriod` | 5 | Lookback period for the %K line. |
| `DPeriod` | 3 | Moving average length for %D. |
| `Slowing` | 3 | Extra smoothing applied to %K before comparisons. |
| `SignalBar` | 1 | Number of closed bars back used to evaluate the signals. |
| `StopLossPoints` | 1000 | Stop-loss distance in instrument steps (set 0 to disable). |
| `TakeProfitPoints` | 2000 | Take-profit distance in instrument steps (set 0 to disable). |
| `EnableLongEntries` | true | Allow the strategy to open long positions. |
| `EnableShortEntries` | true | Allow the strategy to open short positions. |
| `EnableLongExits` | true | Allow closing of long positions when a reversal signal appears. |
| `EnableShortExits` | true | Allow closing of short positions when a reversal signal appears. |
| `Mode` | Twist | Selected signal mode. |

The strategy uses the built-in StockSharp `StochasticOscillator` indicator and feeds it with the configured lengths. The `SignalBar` parameter allows reproducing the MetaTrader behaviour of referencing the previous candle (default = 1) or acting on the latest completed bar when set to 0.

## Trade Management

- Orders are submitted with `BuyMarket` and `SellMarket` calls. Position flips are handled automatically by adding the absolute value of the current position to the base order volume.
- Optional stop-loss and take-profit protection is activated via `StartProtection`. Distances are interpreted as ticks/steps, so they are multiplied by the instrument step size internally by StockSharp.
- No trailing stop is attached; protection remains static until filled or the strategy exits manually.

## Exit Logic

- In **Breakdown** mode, the same threshold break that opens one side closes the other.
- In **Twist** mode, detecting a momentum reversal closes the opposing position before opening the new one.
- In **CloudTwist** mode, %K crossing %D both triggers the entry and simultaneously liquidates the opposite bias.

When entry permissions are disabled, only the corresponding exit logic remains active, which allows users to run the strategy in a "manage existing exposure" mode.

## Implementation Notes

- The oscillator history is tracked in small in-memory buffers so that the strategy can inspect the bar offsets required by the original expert advisor.
- All decisions are evaluated on finished candles only (`candle.State == Finished`).
- Chart rendering draws the underlying candles and the stochastic oscillator when chart services are available.

This conversion maintains the intent of the original MQL5 system while taking advantage of StockSharp's indicator bindings, parameter metadata, and built-in risk controls.
