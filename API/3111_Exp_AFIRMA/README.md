# Exp AFIRMA Strategy

## Overview

The **Exp AFIRMA Strategy** reproduces the MetaTrader expert advisor `Exp_AFIRMA.mq5` using the StockSharp high-level API. The
original system relies on the AFIRMA indicator (Adaptive Finite Impulse Response Moving Average) that blends a windowed FIR
smoother with a short ARMA forecast. The StockSharp version keeps the same market logic: it opens long positions when the ARMA
component turns upward and exits or reverses when the forecast rolls over to the downside.

Trading decisions are made on completed candles from a configurable timeframe (default: H4). The strategy evaluates the ARMA
values of several closed bars to confirm a slope change. Orders are placed at market with optional protective stops and targets
implemented through StockSharp's risk management.

## Trading Logic

1. **Indicator calculation**
   - The built-in `AfirmaIndicator` recreates the two-stage AFIRMA filter. A windowed FIR smoother (length = `Taps`, bandwidth =
     `Periods`) produces a base moving average.
   - The ARMA forecast is computed through the same least-squares coefficients as in the MQL source. The indicator exposes both
     FIR and ARMA values; the strategy only consumes the ARMA component.
2. **Signal evaluation**
   - At every finished candle the most recent ARMA value is stored. The parameter `SignalBar` (default: 1) specifies how many
     already closed bars should be skipped. For example, the default setting analyses bars *[t-1, t-2, t-3]* to trigger at the
     open of bar *t*.
   - **Bullish setup**: previous ARMA value is lower than its predecessor (`ARMA[t-2] < ARMA[t-3]`) and the newest value is above
     the previous (`ARMA[t-1] > ARMA[t-2]`). This closes short exposure and opens/extends a long position if allowed.
   - **Bearish setup**: previous ARMA value is higher than its predecessor while the newest value is below it. This closes long
     exposure and opens/extends a short position if permitted.
3. **Position management**
   - Only one position is maintained. New entries bring the position toward `±TradeVolume`. Existing exposure is flattened before
     flipping.
   - Optional risk protection uses `StartProtection` with price-based stop-loss and take-profit distances.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Base position size used for both long and short entries. |
| `CandleType` | Timeframe/data type requested from the market data adapter (default: 4-hour candles). |
| `Periods` | Reciprocal bandwidth of the FIR stage (`1 / (2 * Periods)`), identical to the original EA input. |
| `Taps` | Number of FIR coefficients. Internally adjusted to the nearest odd value if needed. |
| `Window` | Window function applied to the FIR filter (`Rectangular`, `Hanning1`, `Hanning2`, `Blackman`, `BlackmanHarris`). |
| `SignalBar` | Number of already closed bars to look back for confirmation. `1` corresponds to the last fully closed bar. |
| `EnableBuyEntries` / `EnableSellEntries` | Allow opening of long/short positions. |
| `EnableBuyExits` / `EnableSellExits` | Allow closing of long/short positions on opposite signals. |
| `StopLossPoints` | Optional protective stop expressed in price units. |
| `TakeProfitPoints` | Optional protective target expressed in price units. |

## Notes on the Conversion

- Money-management options (`MM`, `MMMode`, `Deviation_`) from the MetaTrader version are replaced with the simpler
  `TradeVolume` parameter. Use StockSharp portfolio settings for advanced sizing or slippage control.
- The original EA sends stop-loss and take-profit values in points. Here they are provided in absolute price units so that the
  protection module can be reused across different instruments. Convert points to price by multiplying by the appropriate price
  step.
- When `SignalBar = 1` the strategy reads the last three **completed** ARMA values and opens orders on the next bar, matching the
  behaviour of the source code that relies on `CopyBuffer` with shift one. Setting `SignalBar = 0` still works but uses the most
  recently closed bar because StockSharp calculations are performed on finished candles.
- The AFIRMA indicator implementation matches the original math, including the supported window types and coefficient formulas,
  allowing you to display FIR and ARMA lines on a chart if desired.

## Usage Tips

1. Attach the strategy to a security and portfolio, configure `TradeVolume`, and select the candle timeframe through
   `CandleType`.
2. Enable or disable long/short directions according to your trading plan.
3. Set `StopLossPoints` and `TakeProfitPoints` if you want automated risk management; otherwise leave them at zero to trade
   without fixed exits.
4. Monitor the generated chart to verify the AFIRMA lines and the executed trades when tuning `Periods`, `Taps`, and `SignalBar`.
