# Money Rain Strategy

## Overview
- Conversion of the original **MoneyRain (barabashkakvn's edition)** expert advisor from MQL5 to StockSharp high-level API.
- Uses the DeMarker oscillator to choose direction: values above 0.5 trigger long entries, while values at or below 0.5 trigger short entries.
- Trades only one position at a time and relies on fixed stop-loss / take-profit offsets expressed in points.

## Market Data & Indicators
- Subscribes to the configurable `CandleType` (default: 30-minute time frame).
- Computes a single `DeMarker` indicator with adjustable `DeMarkerPeriod` (default: 31).
- Subscribes to Level 1 quotes to approximate the current spread, which is required by the adaptive position-sizing logic.

## Trading Logic
1. Process only finished candles to stay aligned with the original "new bar" logic (`iTime(0)` check in MQL).
2. While a position exists, monitor the candle high/low against pre-computed stop-loss and take-profit levels. If one of them is touched, close the position with a market order and mark the result as either a loss or a profit.
3. When there is no open position and the loss-limit safeguard is not hit, calculate the trade volume.
4. Enter long on `DeMarker > 0.5`; otherwise enter short. The strategy cancels any resting orders before sending the market order.

## Money Management
- Reproduces the `getLots()` logic from the MQL version by tracking:
  - `_lossesVolume`: cumulative volume of recent losing trades scaled by the base lot size.
  - `_consecutiveLosses` and `_consecutiveProfits`: streak counters used to decide when to reset the loss accumulator.
- When the first profitable trade after a losing streak appears (`_consecutiveProfits == 0`), the next order size is increased according to the original formula:
  \[
  \text{volume} = \text{BaseVolume} \times \frac{_lossesVolume \times (\text{StopLossPoints} + \text{spread})}{\text{TakeProfitPoints} - \text{spread}}
  \]
- The spread is estimated from best bid/ask quotes (in points) and ignored when Level 1 data is not yet available.
- Setting `FastOptimize = true` disables the adaptive sizing and always uses the base lot.

## Risk Controls
- `StopLossPoints` and `TakeProfitPoints` are converted to absolute prices using the security price step with an additional 10x multiplier for 3- or 5-digit symbols (mirrors the `digits_adjust` logic from MQL).
- `LossLimit` blocks further trades once the number of consecutive losses exceeds the user-defined threshold (default: effectively disabled at 1,000,000).

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `DeMarkerPeriod` | Averaging period of the DeMarker indicator. | 31 |
| `TakeProfitPoints` | Take-profit offset in DeMarker-style points. | 5 |
| `StopLossPoints` | Stop-loss offset in DeMarker-style points. | 20 |
| `BaseVolume` | Default order volume (lot size). | 0.01 |
| `LossLimit` | Maximum consecutive losses allowed before pausing. | 1,000,000 |
| `FastOptimize` | When `true`, disables adaptive position sizing. | `false` |
| `CandleType` | Candle data type used for calculations. | 30-minute candles |

## Implementation Notes
- Stops and targets are emulated by checking candle extremes. Intrabar fill order cannot be recovered, so simultaneous touches favour the stop-loss branch (conservative assumption).
- `OnOwnTradeReceived` is used to detect when a protective exit order completed, allowing the strategy to update streak counters and loss-volume accumulator.
- The code keeps indentation with tabs and uses English comments, following repository guidelines.

## Files
- `CS/MoneyRainStrategy.cs` – strategy implementation.
- `README.md` / `README_ru.md` / `README_cn.md` – multilingual documentation.

## Differences from the MQL Version
- Broker-side protective orders are replaced with market exits based on candle ranges.
- Spread is approximated from Level 1 quotes rather than directly from symbol metadata.
- Mailing functionality and explicit `IsTradeAllowed` checks are omitted because the StockSharp environment manages connectivity separately.
