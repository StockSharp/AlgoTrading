# Exp RSIOMA V2 Strategy

## Overview
Exp RSIOMA V2 is a conversion of the original MetaTrader 5 expert advisor that trades on the RSIOMA oscillator (Relative Strength Index of Moving Average). The strategy reproduces the same ideas inside the StockSharp high level API: price data are smoothed, converted into a momentum series and fed into an RSI style accumulator. Trading decisions are taken when the oscillator changes direction or crosses predefined zones.

## Trading Logic
1. **Price preprocessing** – the selected candle price (close by default) is smoothed with one of four moving average families (simple, exponential, smoothed or linear weighted).
2. **Momentum calculation** – the smoothed price is compared with the value from `MomentumPeriod` bars ago to obtain the momentum impulse.
3. **RSIOMA computation** – positive and negative momentum components are accumulated with an exponential smoothing of length `RsiomaLength`, producing the RSIOMA value in the `[0; 100]` range.
4. **Signal evaluation** – the most recent closed candles are inspected according to the chosen `Mode`:
   - **Breakdown** – reacts when RSIOMA leaves the main trend levels (`MainTrendLong` / `MainTrendShort`). When the oscillator exits the upper zone, shorts are closed and long entries are permitted; exiting the lower zone performs the opposite action.
   - **Twist** – looks for turning points. A buy occurs when the RSIOMA slope switches from falling to rising, while sells react to a rising-to-falling transition.
   - **CloudTwist** – emulates the coloured cloud logic from the MT5 indicator. Trades are opened when RSIOMA returns from oversold/overbought extremes back inside the channel, and opposite positions are closed at the same time.

Signals are evaluated on the bar specified by `SignalBar` (default: the previous fully closed candle), ensuring that only confirmed data are used.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `OrderVolume` | Default order volume used by market orders. | `1` |
| `CandleType` | Candle data series processed by the strategy. | `4 hour` timeframe |
| `EnableLongEntries` / `EnableShortEntries` | Allow opening new long/short positions. | `true` |
| `EnableLongExits` / `EnableShortExits` | Allow closing existing long/short positions. | `true` |
| `Mode` | Trading logic (Breakdown, Twist or CloudTwist). | `Breakdown` |
| `PriceSmoothing` | Moving average applied to the price before RSIOMA. | `Exponential` |
| `RsiomaLength` | RSIOMA averaging period. | `14` |
| `MomentumPeriod` | Lag between samples when computing momentum. | `1` |
| `AppliedPrice` | Candle price used for the oscillator (close, open, median, DeMark, etc.). | `Close` |
| `MainTrendLong` / `MainTrendShort` | RSIOMA levels that define overbought/oversold zones. | `60` / `40` |
| `SignalBar` | Number of closed bars back that should be analysed. | `1` |

## Implementation Notes
- Only the smoothing families available in StockSharp are supported (simple, exponential, smoothed and linear weighted). Advanced modes from the MT5 version (JJMA, VIDYA, AMA, …) are not included.
- The RSI averages are seeded using the first `RsiomaLength` momentum values to mirror the MetaTrader initialisation. Afterwards an exponential update is applied, matching the original expert advisor behaviour.
- Positions are always closed before an opposite entry is issued. Entry permissions (`EnableLongEntries`, `EnableShortEntries`) and exit permissions (`EnableLongExits`, `EnableShortExits`) provide full control over the allowed directions.
- `SignalBar = 0` can be used to react to the current finished candle; higher values reproduce the MT5 ability to wait several bars before acting.

## Usage
1. Add the strategy to a StockSharp project and assign the instrument you want to trade.
2. Configure the candle subscription through `CandleType` (default is 4-hour candles) and adjust thresholds if the symbol uses different volatility characteristics.
3. Select the preferred signal mode depending on whether you want breakout style entries (`Breakdown`), momentum turns (`Twist`) or cloud colour changes (`CloudTwist`).
4. Start the strategy. During execution the strategy subscribes to the chosen candle series, computes the RSIOMA chain and issues market orders when conditions are satisfied.
