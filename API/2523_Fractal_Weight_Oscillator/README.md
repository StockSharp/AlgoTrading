# Fractal Weight Oscillator Strategy

## Overview
This strategy replicates the "Exp_Fractal_WeightOscillator" expert advisor by
aggregating four oscillators (RSI, Money Flow Index, Williams %R and DeMarker)
into a single smoothed composite signal. The oscillator is compared with two
horizontal levels (`HighLevel`/`LowLevel`) to trigger long or short trades in
either trend-following or counter-trend mode. All calculations are performed on
the selected candle timeframe and use the standard StockSharp high-level API.

## Indicator stack
- **Relative Strength Index** – applied to the configured price source.
- **Money Flow Index** – calculated from the chosen applied price and candle
  volume.
- **Williams %R** – computed from candle high/low/close values.
- **DeMarker** – recreated from candle highs and lows with a simple average
  smoother.
- **Moving average smoother** – optional post-processing of the weighted sum
  (SMA, EMA, SMMA or LWMA).

The composite oscillator value is a weighted average of the four components.
`HighLevel` and `LowLevel` define overbought/oversold zones. `SignalBar`
controls how many completed bars are inspected when looking for a crossing so
you can delay execution relative to the newest finished candle.

## Trading logic
### TrendMode = Direct
- **Long entry / short exit** – when the oscillator falls from above
  `LowLevel` to below or equal `LowLevel` (`BuyOpenEnabled` and
  `SellCloseEnabled` must be true).
- **Short entry / long exit** – when the oscillator rises from below
  `HighLevel` to above or equal `HighLevel` (`SellOpenEnabled` and
  `BuyCloseEnabled` must be true).

### TrendMode = Counter
- **Long entry / short exit** – triggered by an upside break of `HighLevel`.
- **Short entry / long exit** – triggered by a downside break of `LowLevel`.

Signals are evaluated on the bar specified by `SignalBar`. Position reversals
use `Volume + |Position|` to neutralise any existing exposure.

## Risk management
When a new position is opened, the strategy calculates fixed-price stop-loss
and take-profit levels using `StopLossPoints` and `TakeProfitPoints`. The
values are multiplied by the instrument `MinPriceStep`. On every completed
candle the low/high is checked against these targets; if hit, the position is
closed immediately and internal risk trackers are reset.

## Parameters
| Name | Description |
| ---- | ----------- |
| `TrendMode` | Select direct (trend-following) or counter-trend behaviour. |
| `SignalBar` | Number of closed bars back used for signal evaluation. |
| `Period` | Base length for RSI, MFI, Williams %R and DeMarker. |
| `SmoothingLength` | Window for the moving-average smoother. |
| `SmoothingMethod` | Type of moving average (`None`, `Sma`, `Ema`, `Smma`, `Lwma`). |
| `RsiPrice`, `MfiPrice` | Applied price source used in component oscillators. |
| `MfiVolume` | Volume type for MFI (tick and real both use candle volume). |
| `RsiWeight`, `MfiWeight`, `WprWeight`, `DeMarkerWeight` | Relative weights in the composite oscillator. |
| `HighLevel`, `LowLevel` | Upper and lower thresholds for level crossings. |
| `BuyOpenEnabled`, `SellOpenEnabled` | Enable long or short entries. |
| `BuyCloseEnabled`, `SellCloseEnabled` | Allow closing existing positions on opposite signals. |
| `StopLossPoints`, `TakeProfitPoints` | Protective distances in price steps (0 disables the level). |
| `CandleType` | Timeframe of candles passed to the strategy. |
| `Volume` *(Strategy property)* | Trade size used for entries (position reversals add the absolute position). |

## Usage notes
- `SignalBar = 1` reproduces the original expert behaviour by using the last
  fully closed bar. Increasing the value delays reactions by additional bars.
- `SmoothingMethod` allows turning smoothing off (`None`) or matching the
  different moving-average styles available in the MQL version.
- The Money Flow Index implementation always works with the candle total
  volume supplied by the data feed. Both `Tick` and `Real` options therefore
  refer to the same aggregated value because StockSharp candles do not expose
  separate tick counters by default.
- All comments in the C# source are written in English as required.
