# Exp XBullsBearsEyes Vol Direct Strategy

## Overview
This strategy is a C# conversion of the MetaTrader expert **Exp_XBullsBearsEyes_Vol_Direct**. It recreates the custom oscillator
built from Bulls Power and Bears Power, multiplies it by a configurable volume source and applies adaptive smoothing identical to
the original indicator. Trading decisions are driven exclusively by the direction buffer of the indicator: the algorithm reacts
to momentum swings rather than level crossings, opening or closing positions when the smoothed histogram changes slope.

Unlike many conversions, the StockSharp version keeps the volume weighting stage and the four-level gamma filter used by the
MQL code. The oscillator is smoothed twice with the same moving average method—one pass for the histogram itself and one for the
volume stream—so signals appear only when both components are fully formed. The strategy processes finished candles only and
supports tick volume or real traded volume, making it portable across different markets.

## Indicator logic
1. Compute Bulls Power and Bears Power with an exponential moving average of the closing price over `Period` candles.
2. Apply the original four-stage gamma filter (parameters `Gamma`, `L0`–`L3`) to combine the two forces into a normalized
   histogram ranging from -50 to +50.
3. Multiply the histogram by the selected volume source (tick count or traded volume).
4. Smooth the histogram and the raw volume with the same moving average family (`Method`, `SmoothingLength`, `SmoothingPhase`).
5. Derive a direction buffer: color `0` when the smoothed histogram rises, color `1` when it falls. This mimics the
   `ColorDirectBuffer` from the MetaTrader implementation.

The higher/lower threshold buffers from the indicator are calculated internally but are not used for trade filters, matching the
behaviour of the original expert advisor that relied only on direction flips.

## Trading rules
- **Close shorts** when the previous bar’s direction was bullish (`olderColor = 0`).
- **Open longs** if long entries are allowed, a bullish bar is followed by a bearish one (`currentColor = 1`), and the strategy is
  not already long.
- **Close longs** when the previous bar’s direction was bearish (`olderColor = 1`).
- **Open shorts** if short entries are allowed, a bearish bar is followed by a bullish one (`currentColor = 0`), and no long
  position is active.
- Position reversals close the opposite side first and then submit a market order with the configured `OrderVolume`.

Signals are evaluated with a configurable bar shift (`SignalBar`). The default value of `1` emulates the MQL expert that waited
for a fully closed candle before reacting to the direction change.

## Parameters
| Name | Description |
|------|-------------|
| `CandleType` | Candle type/timeframe subscribed by the strategy (default: 2-hour candles). |
| `Period` | Lookback period used for Bulls/Bears Power. |
| `Gamma` | Smoothing factor (0…1) of the adaptive gamma filter. |
| `VolumeMode` | Volume source: tick count or traded volume. |
| `Method` | Moving average family used to smooth histogram and volume (SMA, EMA, SMMA, LWMA, Jurik; unsupported legacy types fall back to SMA). |
| `SmoothingLength` | Length of both smoothing stages. |
| `SmoothingPhase` | Jurik phase parameter (kept for compatibility). |
| `SignalBar` | Number of bars back to read when evaluating the direction buffer. |
| `AllowBuyOpen` / `AllowSellOpen` | Enable or disable opening long/short positions. |
| `AllowBuyClose` / `AllowSellClose` | Enable or disable forced exits on opposite signals. |
| `OrderVolume` | Market order size used for new entries. |
| `StopLossPoints` | Optional protective stop in price steps (0 disables the stop). |
| `TakeProfitPoints` | Optional protective target in price steps (0 disables the target). |

## Usage notes
- The strategy operates on a single security returned by `GetWorkingSecurities()` and works best on symbols that provide a stable
  volume stream.
- Tick volume is recommended for spot FX symbols where real traded volume is unavailable. Set `VolumeMode` to `Real` for
  exchanges that publish executed volume.
- Stop-loss and take-profit distances are expressed in price steps and converted into absolute price units using the security’s
  `PriceStep`.
- Because the logic relies on direction flips, consecutive equal histogram values keep the previous direction until a new slope
  appears, exactly like the MetaTrader version.
- Chart output plots only price candles by default. You can add custom plotting for the histogram if visual confirmation is
  required.
