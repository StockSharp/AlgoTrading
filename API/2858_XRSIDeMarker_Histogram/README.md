# XRSI DeMarker Histogram Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Summary
This strategy replicates the **Exp_XRSIDeMarker_Histogram** expert advisor. It trades reversals detected by a custom oscillator that blends a Relative Strength Index (RSI) with the DeMarker indicator and then smooths the result. The system can open or close long and short trades independently, and optional protective stops expressed in price steps are supported.

## Indicator construction
1. **Applied price** – the RSI is calculated on the selected input (close, open, high, low, median, typical or weighted price) using the configured period.
2. **DeMarker component** – for each finished candle, the strategy measures the upward (`deMax`) and downward (`deMin`) pressure:
   - `deMax = max(High_t - High_{t-1}, 0)`
   - `deMin = max(Low_{t-1} - Low_t, 0)`
   Both series are smoothed with a simple moving average whose length matches the RSI period.
   - `DeMarker = deMaxAvg / (deMaxAvg + deMinAvg)` (scaled to the 0–100 range).
3. **Composite oscillator** – the final value is `(RSI + 100 * DeMarker) / 2`.
4. **Smoothing** – the composite oscillator is passed through one of the supported moving averages (SMA, EMA, SMMA, LWMA or Jurik). If an unsupported smoothing mode from the original MQL version is selected, the indicator falls back to an EMA with the requested length. The Jurik option also honours the phase parameter.
5. **Signal history** – the strategy stores historical values and evaluates signals on the bar defined by `SignalBar`, mimicking the original EA which waited for the next candle before taking trades.

## Trading logic
- **Bullish reversal**
  - Condition: value at `SignalBar+1` is below `SignalBar+2` (down-slope) and the value at `SignalBar` turns back up (`>=`).
  - Actions:
    - Close existing short trades when `CloseShortOnLongSignal` is true.
    - Open a new long trade with `TradeVolume` (plus any quantity required to flip from a short) when `AllowBuyEntries` is enabled.
- **Bearish reversal**
  - Condition: value at `SignalBar+1` is above `SignalBar+2` (up-slope) and the value at `SignalBar` turns down (`<=`).
  - Actions:
    - Close existing long trades when `CloseLongOnShortSignal` is true.
    - Open a new short trade when `AllowSellEntries` is enabled.
- Signals are ignored until the indicator and DeMarker components are fully formed, and orders are placed only when the strategy is online and trading is permitted.

## Risk management
- `StopLossTicks` and `TakeProfitTicks` represent distances in **price steps**. The strategy multiplies these values by `Security.PriceStep` (falling back to `1` if the instrument step is unknown) and closes the position when the distance is reached inside the candle range.
- Passing `0` disables the respective protection.
- The `TradeVolume` parameter is used as the default order size and also to compute reversals (the opposite position is closed before a new one is opened).

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `TradeVolume` | Volume used when opening new positions. | `0.1` |
| `StopLossTicks` | Protective stop in price steps. | `1000` |
| `TakeProfitTicks` | Profit target in price steps. | `2000` |
| `AllowBuyEntries` | Enable/disable entering long trades. | `true` |
| `AllowSellEntries` | Enable/disable entering short trades. | `true` |
| `CloseLongOnShortSignal` | Close longs when a short signal appears. | `true` |
| `CloseShortOnLongSignal` | Close shorts when a long signal appears. | `true` |
| `CandleType` | Timeframe used for analysis (default 4-hour candles). | `H4` |
| `IndicatorPeriod` | Look-back for both RSI and DeMarker components. | `14` |
| `AppliedPriceSelection` | Applied price used by the RSI calculation. | `Close` |
| `SmoothingMethodSelection` | Moving average used for smoothing (SMA/EMA/SMMA/LWMA/Jurik/Adaptive). | `Sma` |
| `SmoothingLength` | Period of the smoothing average. | `5` |
| `SmoothingPhase` | Phase argument passed to Jurik smoothing. | `15` |
| `SignalBar` | Number of closed bars back used for signal evaluation. | `1` |

## Notes vs. original EA
- Money management modes from the MQL version (balance-based, free-margin-based, etc.) are replaced with a direct `TradeVolume` parameter.
- Order slippage (`Deviation`) is not required because StockSharp uses market orders.
- Advanced smoothing algorithms (Parabolic MA, T3, VIDYA, AMA) are not available in StockSharp and are mapped to the EMA via the `Adaptive` option.
- All comments in the C# source are written in English, and the logic runs only on finished candles just like the original implementation.
