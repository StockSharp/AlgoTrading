# Trend Me Leave Me Strategy

## Overview
The **Trend Me Leave Me** strategy is a direct port of the classic MQL5 expert advisor by Yury Reshetov. It patiently waits for
periods of quiet price action, joins the prevailing direction indicated by the Parabolic SAR, and alternates the trade direction
after profitable exits. When a trade is stopped out, the strategy will attempt the same direction again, recreating the original
"trend me, leave me" behaviour. This C# implementation uses the StockSharp high-level API and keeps the full decision flow of the
source system while exposing every numeric input as a configurable parameter.

## Core Ideas
### Calm-market filter
- The Average Directional Index (ADX) with `AdxPeriod` length measures directional strength.
- Only when the ADX moving average drops below `AdxQuietLevel` does the strategy allow new entries, mimicking the EA's focus on
  low-volatility pullbacks.

### SAR alignment for timing
- Parabolic SAR points act as the directional guide. A long signal requires the candle close to print above the SAR dot, whereas
  a short signal requires a close below the dot.
- The SAR parameters `SarStep` and `SarMax` match the acceleration settings from the MQL version and may be optimised if needed.

### Direction scheduler
- A `TradeDirection` flag represents the original `cmd` variable. It starts in the *buy* state.
- After a **take-profit** exit the flag flips to the opposite side, inviting a reversal trade.
- After a **stop-loss** (or breakeven) exit the flag remains on the same side so that the next opportunity retries the previous
  direction.

## Trade Management
- `StopLossPips` and `TakeProfitPips` define fixed distances from the average fill price. Setting either parameter to `0`
  disables the corresponding protection.
- `BreakevenPips` moves the stop to the entry price once the market travels in favour by the specified pip distance. If price
  later returns to the entry level the trade is closed for roughly zero profit, which keeps the next signal on the same side.
- The stop/take logic is evaluated on every completed candle using both the high and low to approximate intrabar hits, preserving
  the tick-by-tick behaviour of the EA as closely as possible in a bar-driven environment.

## Position Sizing
- Order volume is controlled by the base `Strategy.Volume` property. The sample keeps the risk model simple and does not include
the fixed-risk money management object from the MQL script. Adjust `Volume` or override the strategy if more advanced sizing is
required.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `StopLossPips` | Distance in pips between the entry price and the protective stop. | `50` |
| `TakeProfitPips` | Distance in pips between the entry price and the target. | `180` |
| `BreakevenPips` | Move the stop to entry after this many pips of favourable movement. | `5` |
| `AdxPeriod` | Smoothing period for the ADX filter. | `14` |
| `AdxQuietLevel` | Maximum ADX reading that still qualifies as a quiet market. | `20` |
| `SarStep` | Parabolic SAR acceleration step. | `0.02` |
| `SarMax` | Parabolic SAR maximum acceleration factor. | `0.2` |
| `CandleType` | Time frame used for calculations. | `1h` candles |

## Implementation Notes
- Pip calculations follow the EA's digit adjustment: if the security uses 3 or 5 decimal places the price step is multiplied by
  10 to convert the broker tick size into a standard pip.
- Indicator bindings rely on the StockSharp high-level API, and all trading actions use `BuyMarket`/`SellMarket` to stay in line
  with the S# conventions.
- No Python translation is included yet. The `PY/` directory is intentionally absent as requested.
- Attach the strategy to any symbol supported by StockSharp. Set the `Volume` before starting the strategy and adjust parameters
  to match the instrument's volatility.
