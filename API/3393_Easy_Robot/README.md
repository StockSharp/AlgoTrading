# Easy Robot Strategy

## Overview
Easy Robot is a momentum-following Expert Advisor that trades once per completed hourly candle. When the previous candle closes bullish the strategy opens a new long position; when it closes bearish it opens a short. Only one position can be active at any time, fully mirroring the original MetaTrader 4 logic.

## Trade Rules
1. Subscribe to the hourly candle type selected by the **CandleType** parameter (defaults to H1).
2. Once a candle is finished, compare its close with the open:
   - Close > Open: send a market buy order if no position is open.
   - Close < Open: send a market sell order if flat.
3. The position size uses the strategy `Volume` property, exactly like the MQL version that relied on `CheckVolumeValue` with a default of 0.01 lots.
4. Stop-loss and take-profit levels rely on an **Average True Range** indicator with period **AtrPeriod** (default 14):
   - Stop distance = `ATR * StopFactor`.
   - Take distance = `ATR * TakeFactor`.
   - Both distances are normalised by the minimal tick/pip distance so protective orders are never placed closer than the broker allows.
5. Protective orders are registered immediately after the market order through `SetStopLoss` and `SetTakeProfit`, providing the same behaviour as `OrderSend` with `sl` and `tp` parameters.
6. Optional trailing is activated when **UseTrailingStop** is true. After the trade accumulates **TrailingStartPips** profit (MetaTrader pips, i.e. points adjusted for 3/5 decimal quotes), the stop is moved closer by **TrailingStepPips** and is pushed further only when new profit extremes are reached. Trailing respects the broker’s minimal stop distance to avoid invalid modifications.
7. Quotes for stop calculations use the best bid/ask when available, falling back to the last price or candle close, matching the original `Bid`/`Ask` references.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `TakeFactor` | 4.2 | ATR multiplier for take-profit distance (maps to `TakeFactor` input in MQL). |
| `StopFactor` | 4.9 | ATR multiplier for stop-loss distance (maps to `StopFactor`). |
| `UseTrailingStop` | true | Enables MetaTrader-style trailing (`UseTstop`). |
| `TrailingStartPips` | 40 | Profit in pips before trailing can start (`Tstart`). |
| `TrailingStepPips` | 19 | Pip step applied when trailing updates (`Tstep`). |
| `AtrPeriod` | 14 | ATR calculation period for volatility sizing. |
| `CandleType` | H1 | Candle series used for signals and ATR input. |

## Notes
- The strategy resets stored entry and stop prices whenever the position returns to zero, ensuring a clean state for the next signal.
- Minimal stop distance is estimated via the instrument pip size (or price step when pip size is not available). This reproduces the `SC` helper from the MQL include file.
- `StartProtection()` is called once at start so the platform can manage emergency exits if needed.
