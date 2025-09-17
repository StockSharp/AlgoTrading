# Exp Blau HLM Strategy

## Overview

The **Exp Blau HLM Strategy** is a StockSharp port of the MetaTrader 5 expert advisor `Exp_BlauHLM.mq5`. The system relies on the Blau High-Low Momentum (HLM) oscillator that compares recent highs and lows, smooths the difference with a configurable XMA pipeline and reacts to three discrete operating modes:

- **Breakdown** – trades a zero-line break of the histogram component.
- **Twist** – searches for momentum twists inside the histogram to capture transitions in slope.
- **CloudTwist** – works with the upper and lower envelopes produced by the indicator and reacts to "cloud" crossovers.

The StockSharp implementation keeps the same parameters, default values and trading rules while translating money-management specifics into the generic `Volume` property of the base strategy.

## Trading Logic

1. For every finished candle of the configured timeframe the strategy calculates the Blau HLM oscillator:
   - Compute the difference between the most recent high and the high `XLength - 1` bars ago and a mirrored difference for lows.
   - Clamp negative contributions to zero and subtract them to obtain the raw HLM value (expressed in points when the instrument specifies a tick size).
   - Smooth the sequence through four cascaded moving averages with identical methods but independent lengths.
2. Depending on the selected **Mode**:
   - **Breakdown** opens a long position when the older histogram value is positive and the newer one is non-positive (zero-line recovery) and closes shorts in the same situation. A symmetric rule handles short entries/long exits when the histogram switches from negative to non-negative.
   - **Twist** compares the histogram slope across three historical points. A local acceleration (middle value rising after a decline) triggers long logic, while a deceleration (middle value falling after a rise) activates short logic.
   - **CloudTwist** monitors the two smoothed envelopes. When the older upper band is above the lower band and the newer values cross below/above each other, long or short signals are produced accordingly.
3. Position management follows the permissions `BuyOpen`, `SellOpen`, `BuyClose`, `SellClose` and uses the strategy `Volume` for market entries. Opposite signals close existing positions before opening a new one.

## Parameters

| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `CandleType` | `DataType` | `H4` candles | Timeframe processed by the oscillator. |
| `SmoothingMethod` | `SmoothMethod` | `Exponential` | Moving-average method for every smoothing stage (unsupported legacy modes fall back to EMA). |
| `XLength` | `int` | `2` | Span, in bars, used to measure the raw high/low momentum. |
| `FirstLength` | `int` | `20` | Period of the first smoothing stage. |
| `SecondLength` | `int` | `5` | Period of the second smoothing stage. |
| `ThirdLength` | `int` | `3` | Period of the third smoothing stage. |
| `FourthLength` | `int` | `3` | Period of the final signal smoother. |
| `Phase` | `int` | `15` | Jurik phase parameter (clamped to ±100, ignored by non-Jurik smoothers). |
| `SignalBar` | `int` | `1` | Historical offset used when comparing indicator values. |
| `EntryMode` | `Mode` | `Twist` | Trading logic copied from the MQL expert (`Breakdown`, `Twist`, `CloudTwist`). |
| `BuyOpen` / `SellOpen` | `bool` | `true` | Allow opening long/short positions. |
| `BuyClose` / `SellClose` | `bool` | `true` | Allow closing long/short positions when an opposite signal appears. |

## Conversion Notes

- The MQL library `SmoothAlgorithms.mqh` includes proprietary filters (JJMA, JurX, ParMA, T3, VIDYA, AMA). StockSharp provides built-in alternatives for the most common variants, therefore unsupported modes are approximated with the exponential moving average to keep the workflow intact.
- Money-management parameters (`MM`, `MarginMode`, `StopLoss`, `TakeProfit`, `Deviation`) control order size and execution on MetaTrader. In this port the generic `Volume` property defines position size and orders are always sent at market.
- Signal timing mirrors the `SignalBar` offset used by the original expert: the strategy keeps an internal circular buffer of indicator values and performs comparisons on historical snapshots so that optimization results remain consistent.
- Risk protection is delegated to `StartProtection()`; configure global stop-loss/take-profit rules on the parent strategy or trading connector if required.

## Usage Tips

1. Set the `Volume` property before starting the strategy to define the number of lots/contracts per trade.
2. For symbols without a meaningful `PriceStep`, the oscillator works in raw price units—consider rescaling the parameters if the asset uses large tick sizes.
3. When experimenting with non-exponential smoothers remember that very short lengths combined with Jurik phase extremes may lead to choppy signals; widen the periods for stability.
4. Combine the strategy with portfolio-level risk controls or the built-in protection rules to emulate the original stop-loss/take-profit behaviour.

