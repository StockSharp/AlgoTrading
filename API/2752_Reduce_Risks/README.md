# Reduce Risks Strategy

## Overview
The Reduce Risks Strategy is a multi-timeframe trend-following system converted from the MetaTrader expert advisor "Reduce_risks.mq5". It analyses one-minute candles to trigger entries while filtering the market regime with 15-minute and 1-hour averages. The original algorithm was designed for highly liquid forex majors (EURUSD, USDCHF, USDJPY) and focuses on entering trends only when volatility is muted and structure confirms continuation.

## Market and Timeframes
- **Primary timeframe:** 1-minute candles for signal generation.
- **Confirmation timeframe:** 15-minute candles for momentum validation and wave positioning.
- **Trend filter:** 1-hour candles to ensure trading in the broader trend direction.
- **Recommended instruments:** EURUSD, USDCHF, USDJPY or instruments with similar pip structure (4 or 5 decimal pricing).

## Indicators and Data
- Four simple moving averages (SMA) on M1: periods 5, 8, 13 and 60 calculated on typical price.
- Three SMAs on M15: periods 4, 5 and 8 calculated on typical price.
- One SMA on H1: period 24 calculated on typical price.
- Candlestick statistics (body size, range, shadows) for both M1 and M15.
- Internal counters track the highest or lowest price since entry to emulate the MQL trailing logic.

## Entry Rules
### Long setup
1. Recent M1 and M15 candles must display low volatility: three previous bars on each timeframe have ranges below 20 and 30 pips respectively, and the 15-minute channel width is capped at 30 pips.
2. The latest completed M1 candle is more active than its predecessor but not three times larger, and the current price breaks both the recent M1 and M15 highs (local resistance cleared).
3. SMA hierarchy points upward: SMA5 > SMA8 > SMA13 and SMA60 rising; the closing price sits above all four averages.
4. SMA4 on M15 is rising and positioned above SMA8, while the closing price is above both the M15 and H1 averages.
5. Wave confirmation: SMA8 on M1 crossed inside any of the previous three candles, and SMA5 on M15 lies within the prior M15 candle range.
6. Candle structure filters: previous M1 and M15 candles have bullish bodies exceeding half of their ranges, maintain higher highs, show acceptable pullbacks (<25% of the previous candle range), and contain intrabar shadows (no marubozu).
7. All conditions above must be satisfied simultaneously with no open position before issuing a market buy order.

### Short setup
1. The same volatility filters apply, but the breakout must occur below recent lows (support violation).
2. SMA hierarchy flips: SMA5 < SMA8 < SMA13 with SMA60 falling; the closing price sits below all four averages.
3. SMA4 on M15 declines and is below SMA8; the closing price is below both the M15 and H1 averages.
4. Wave validation: SMA8 on M1 lies within any of the previous three M1 candle ranges, SMA5 on M15 resides inside the last M15 candle, and recent candles show persistent bearish structure (lower lows, bearish bodies, limited pullbacks, shadows present).
5. With no active position, a market sell order is sent once all conditions align.

## Exit Rules
- Protective stop-loss and take-profit orders are attached automatically using the configured pip distances (mirrors the original EA behaviour).
- Additional discretionary exits replicate the MQL logic:
  - Close longs if the current M1 candle collapses by at least 10 pips from its open or if a strong bearish M1 candle appears after the trade has been open for more than one minute.
  - Take profit early when price advances at least 10 pips, or when a trailing reversal occurs: after the first bar following entry, if price retraces 20 pips from the highest level reached since entry while that high sits above the entry price.
  - Close longs on a 20 pip adverse excursion or whenever the portfolio equity falls below the configured drawdown threshold. Short positions use symmetrical logic with inverted comparisons.

## Risk Management
- Trading halts automatically when the portfolio equity drops below `(InitialDeposit * (100% - RiskPercent))`. The limit is checked on every signal attempt and reset once equity recovers above the threshold.
- The original MQL script included extensive terminal checks; those are omitted because StockSharp handles connectivity and permissions natively.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `StopLossPips` | Protective stop distance in pips (mirrored by trailing logic). | `30` |
| `TakeProfitPips` | Take-profit distance in pips. | `60` |
| `InitialDeposit` | Reference equity used to compute the drawdown stop. | `10000` |
| `RiskPercent` | Maximum percentage of the initial deposit that can be lost before blocking new trades and force-closing active positions. | `5` |
| `M1CandleType` | Data type for the 1-minute candle subscription. | `1 minute` time-frame |
| `M15CandleType` | Data type for the 15-minute confirmation subscription. | `15 minutes` time-frame |
| `H1CandleType` | Data type for the 1-hour trend filter subscription. | `1 hour` time-frame |

## Notes
- The strategy expects instruments quoted with pip sizes similar to major forex pairs. Adjust the pip-based parameters when using other markets.
- Only the C# implementation is provided; the Python version is intentionally omitted per requirements.
