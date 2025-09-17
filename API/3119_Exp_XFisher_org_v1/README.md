# Exp XFisher org v1 Strategy

## Overview
The strategy reproduces the MetaTrader 5 expert **Exp_XFisher_org_v1**. It trades reversals detected on the Fisher transform of
price that is additionally smoothed with a configurable moving average. The StockSharp port keeps the counter-trend nature of
the original robot: when the Fisher curve turns downward after an upswing a long position is opened, and when the curve turns
upward after a downswing a short position is opened. Existing positions are closed once the indicator reverses in the opposite
direction.

The helper indicator `XFisherOrgIndicator` implemented in `CS/ExpXFisherOrgV1Strategy.cs` follows the MT5 logic:

1. Take the highest high and lowest low over `Length` finished candles.
2. Convert the selected price source (see *Applied Price* below) into the 0–1 range using those extremes.
3. Apply the recursive filter `value = (wpr - 0.5) + 0.67 * value[prev]` followed by the Fisher transform
   `fish = 0.5 * ln((1 + value) / (1 - value)) + 0.5 * fish[prev]`.
4. Smooth the result with one of the supported moving averages. The smoothed Fisher value forms the main line; the signal line
   is simply the previous bar’s smoothed value, exactly as in the MQL version where buffer #1 stores a one-bar shift.

The conversion keeps the original defaults (`Length = 7`, Jurik smoothing of length 5, phase 15, H4 candles) and exposes the
same enable/disable switches for opening and closing long/short trades.

## Trading rules
- **Long entry** – when the Fisher value from `SignalBar + 1` bars ago was rising (`Fisher[SignalBar+1] > Fisher[SignalBar+2]`)
  but the value at `SignalBar` crosses below or touches its delayed copy (`Fisher[SignalBar] <= Fisher[SignalBar+1]`).
- **Short entry** – when the Fisher value from `SignalBar + 1` bars ago was falling but the value at `SignalBar` crosses above
  its delayed copy.
- **Position exit** – the opposite reversal closes an existing position before considering a new trade. A long exit is triggered
  by the same condition that opens a short, and vice versa.
- **Volume** – controlled by `OrderVolume`. When a flip from short to long (or long to short) is required the strategy sends a
  single market order with enough volume to close the old position and open the new one in the same transaction, mimicking the
  behaviour of the original `BuyPositionOpen`/`SellPositionOpen` helpers.

All calculations use **finished candles only**. If `SignalBar` is zero the current closed candle is used for signal evaluation;
positive values shift the signal back in time exactly like the MT5 `SignalBar` input.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `OrderVolume` | Volume of every market order. | `1` |
| `BuyOpenAllowed` / `SellOpenAllowed` | Permit opening long/short trades. | `true` |
| `BuyCloseAllowed` / `SellCloseAllowed` | Permit closing existing long/short trades. | `true` |
| `SignalBar` | Shift (in closed candles) used to read the Fisher buffers. | `1` |
| `Length` | Lookback for highest/lowest price extremes. | `7` |
| `SmoothingLength` | Period of the smoothing average. | `5` |
| `Phase` | Jurik phase (ignored by other methods). | `15` |
| `SmoothingMethod` | Moving average applied to the Fisher output. | `Jjma` |
| `PriceType` | Applied price forwarded to the indicator (close, open, median, etc.). | `Close` |
| `CandleType` | Candle series used for the calculation (default: 4 hour candles). | `H4` |

## Smoothing method mapping
The original indicator exposes a large set of smoothing kernels. The StockSharp port maps them to reliable built-in
implementations:

- `Jjma`, `Jurx`, `T3` → `JurikMovingAverage` (phase parameter applied when the property is available).
- `Sma`, `Ema`, `Smma`, `Lwma` → respective StockSharp moving averages.
- `Parabolic` → approximated by `ExponentialMovingAverage` (closest behaviour under StockSharp).
- `Vidya`, `Ama` → `KaufmanAdaptiveMovingAverage` (the adaptive VIDYA behaviour is modelled with Kaufman AMA).

This mapping mirrors the approach used in other Kositsin conversions inside the repository and keeps the response of the
smoothed Fisher line comparable to the MT5 implementation.

## Differences from the MT5 expert
- **Money management** – StockSharp strategies operate on explicit volumes. The `MM`/`MarginMode` inputs from MT5 are replaced
  with a single `OrderVolume` parameter so the trader can define the lot size directly.
- **Execution model** – trades are generated once per finished candle via the high-level subscription API instead of on every
  tick. This avoids duplicate orders and removes the need for the original `IsNewBar` helper.
- **Applied price options** – all price modes from `SmoothAlgorithms.mqh` are supported, including TrendFollow and Demark
  variants.
- **Charting** – the strategy draws candles, the smoothed Fisher transform and the executed trades in the default chart area.

## Files
- `CS/ExpXFisherOrgV1Strategy.cs` – strategy class, indicator implementation and value container.

