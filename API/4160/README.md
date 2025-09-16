# New FSCEA Strategy

## Overview
The New FSCEA strategy is a MACD-based trend following system that was ported from the original MetaTrader 4 expert advisor `new_fscea.mq4`. The strategy combines a classic MACD crossover confirmation with an EMA slope filter, static take-profit targets, and a trailing stop to manage open positions. It trades a single symbol at a time and opens only one position in the market.

## Trading Logic
### Long Entry
- MACD main line is below zero, but crosses above the signal line on the current closed candle.
- The previous candle still had the MACD line below the signal line (confirms the crossover).
- The absolute value of the MACD line exceeds the `OpenLevelPoints` threshold (scaled by price step).
- The shifted EMA slope is positive (`EMA_shifted_now > EMA_shifted_previous`).
- No position is currently open.

### Short Entry
- MACD main line is above zero, but crosses below the signal line on the current closed candle.
- The previous candle still had the MACD line above the signal line.
- The MACD main line exceeds the `OpenLevelPoints` threshold (scaled by price step).
- The shifted EMA slope is negative (`EMA_shifted_now < EMA_shifted_previous`).
- No position is currently open.

### Long Exit
- Triggered when MACD crosses below the signal line while staying above zero and the MACD value exceeds the `CloseLevelPoints` threshold.
- Or when the candle high touches the virtual take-profit level (`entry + TakeProfitPoints * priceStep`).
- Or when the candle low reaches the trailing-stop level (updated dynamically as price moves in favor).

### Short Exit
- Triggered when MACD crosses above the signal line while staying below zero and the absolute MACD value exceeds the `CloseLevelPoints` threshold.
- Or when the candle low touches the virtual take-profit level (`entry - TakeProfitPoints * priceStep`).
- Or when the candle high reaches the trailing-stop level (updated dynamically as price moves in favor).

## Risk Management
- Take profit is expressed in instrument points and converted into price by multiplying by `Security.PriceStep`.
- Trailing stop works in points and tightens once the floating profit is larger than the trailing distance.
- Only one position can be open at any time, mirroring the behaviour of the MT4 expert advisor.
- Position protection is enabled through the built-in `StartProtection()` helper.

## Indicators
- **MACD (12, 26, 9)** – the main crossover engine. The histogram magnitude provides the entry and exit thresholds.
- **EMA (TrendPeriod)** – applied to close prices. The slope comparison uses a configurable shift (`TrendShift`) to emulate the MT4 `ma_shift` parameter.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `TakeProfitPoints` | 300 | Distance to the profit target in points. Converted to price using the symbol price step. |
| `TrailingStopPoints` | 20 | Trailing stop size in points. Activated only after the trade moves in favor by more than this distance. |
| `OpenLevelPoints` | 3 | Minimum MACD magnitude (points) required before a new trade is allowed. |
| `CloseLevelPoints` | 2 | MACD magnitude (points) required to close a trade via MACD crossover. |
| `TrendPeriod` | 10 | Length of the EMA trend filter. |
| `TrendShift` | 2 | Horizontal shift (in bars) applied to the EMA when evaluating its slope. Higher values delay trend confirmation. |
| `TradeVolume` | 0.1 | Default order volume sent with market orders. |
| `CandleType` | 1-hour time frame | Candle type used for indicator calculations; can be changed to match the desired timeframe. |

## Implementation Notes
- The strategy processes only finished candles to keep the logic close to the MT4 version.
- EMA shift is emulated by buffering indicator outputs and comparing values `TrendShift` bars apart.
- Trailing stop and take profit are implemented virtually (no actual stop/limit orders) to stay within the high-level API requirements.
- The code relies exclusively on the high-level candle subscription API (`SubscribeCandles().BindEx(...)`) to comply with repository guidelines.
