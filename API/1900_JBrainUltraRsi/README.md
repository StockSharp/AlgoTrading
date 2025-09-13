# JBrainUltraRSI Strategy

This sample strategy combines the Relative Strength Index (RSI) and the Stochastic oscillator to generate trading signals.
The idea is derived from the original MetaTrader Expert Advisor that used the *JBrainTrendSig1* and *UltraRSI* indicators. In this adaptation the Stochastic oscillator acts as a trend filter while RSI provides entry signals.

## How It Works

1. **Indicators**
   - **RSI**: Measures momentum by comparing recent gains and losses. A cross above the 50 level indicates bullish momentum, while a cross below 50 indicates bearish momentum.
   - **Stochastic Oscillator**: Evaluates the position of the close relative to the recent range. Crosses of %K and %D lines confirm trend direction.
2. **Modes**
   - **JBrainSig1Filter** – RSI generates signals and the Stochastic oscillator confirms direction.
   - **UltraRsiFilter** – Stochastic oscillator provides signals filtered by RSI.
   - **Composition** – Signals are taken only when both indicators agree on direction.
3. **Trading Rules**
   - A long position opens when a buy signal appears and short position is absent or closed.
   - A short position opens when a sell signal appears and long position is absent or closed.
   - Reverse signals close existing positions if allowed.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `RsiPeriod` | RSI calculation period. |
| `StochLength` | %K period for the Stochastic oscillator. |
| `SignalLength` | %D period for the Stochastic oscillator. |
| `Mode` | Mode of combining indicators. |
| `AllowLongEntry` / `AllowShortEntry` | Permissions to open long or short positions. |
| `AllowLongExit` / `AllowShortExit` | Permissions to close long or short positions. |
| `CandleType` | Candle timeframe used by the strategy. |

## Notes

- The strategy uses StockSharp high level API with `Bind` / `BindEx` for indicator processing.
- Stops and targets can be configured with the built-in protection mechanism `StartProtection()`.
- Example visualisation draws candles, indicators and own trades if a chart area is available.
