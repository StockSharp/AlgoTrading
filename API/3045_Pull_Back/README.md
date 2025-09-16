# Pull Back Strategy

## Overview

The Pull Back strategy reproduces the logic of the original MetaTrader "PULL BACK" expert advisor using StockSharp high-level APIs. The approach searches for pullbacks into a fast weighted moving average on a higher timeframe, confirms momentum strength across several bars, and trades in the direction of the monthly MACD trend. Once a position is open, the algorithm applies money management rules that include stop-loss, take-profit, break-even and trailing stop handling.

## Data and indicators

- **Trading timeframe:** user-selectable candle type (`CandleType`, default 15-minute candles).
- **Confirmation timeframe:** higher timeframe subscription (`HigherCandleType`, default 1-hour candles) used for:
  - Fast/slow weighted moving averages.
  - Momentum indicator with absolute distance from the neutral value (100).
  - Pull-back detection when the previous candle touches the fast WMA.
- **MACD timeframe:** separate subscription (`MacdCandleType`, default 30-day candles) to read the MACD signal line direction.
- **Indicators:**
  - Weighted Moving Average (WMA) on trading and higher timeframes.
  - Momentum (period configurable) on the higher timeframe.
  - Moving Average Convergence Divergence (MACD) on the long timeframe.

## Trading logic

### Long setup

1. The higher timeframe fast WMA is above the slow WMA.
2. The most recent completed higher timeframe candle opened above the fast WMA and touched it with its low (pull-back confirmation).
3. At least one of the last three absolute momentum readings exceeds `MomentumBuyThreshold`.
4. MACD main line is above its signal line on the MACD timeframe.
5. On the trading timeframe, the fast WMA is above the slow WMA.

When all rules are satisfied the strategy sends a market buy order. The entry price is recorded to control risk parameters.

### Short setup

1. The higher timeframe fast WMA is below the slow WMA.
2. The recent candle opened below the fast WMA and touched it with its high.
3. One of the last three momentum values exceeds `MomentumSellThreshold`.
4. MACD main line is below the signal line.
5. The trading timeframe fast WMA is below the slow WMA.

A market sell order is sent when conditions align.

## Position management

- **Stop loss:** `StopLossTicks` distance from the entry (converted to absolute price using the security price step).
- **Take profit:** `TakeProfitTicks` distance from the entry.
- **Break-even:** when price advances by `BreakEvenTriggerTicks`, the stop is moved to entry plus `BreakEvenOffsetTicks` in the trade direction if `UseBreakEven` is enabled.
- **Trailing stop:** if `UseTrailingStop` is true, the stop follows price by `TrailingStopTicks` once the position moves in profit.
- **Exit checks:** run on every finished trading timeframe candle. If stop or target is reached the strategy closes the entire position with a market order.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `FastMaLength` | Fast WMA length on the trading timeframe (default 6). |
| `SlowMaLength` | Slow WMA length on the trading timeframe (default 85). |
| `BounceSlowLength` | Slow WMA length on the confirmation timeframe (default 200). |
| `MomentumLength` | Momentum lookback on the higher timeframe (default 14). |
| `MomentumBuyThreshold` | Minimum |Momentum-100| for long entries (default 0.3). |
| `MomentumSellThreshold` | Minimum |Momentum-100| for short entries (default 0.3). |
| `StopLossTicks` | Stop-loss distance in ticks (default 200). |
| `TakeProfitTicks` | Take-profit distance in ticks (default 500). |
| `UseTrailingStop` | Enable trailing stop logic (default true). |
| `TrailingStopTicks` | Trailing stop distance in ticks (default 400). |
| `UseBreakEven` | Enable break-even adjustment (default true). |
| `BreakEvenTriggerTicks` | Profit trigger for break-even in ticks (default 300). |
| `BreakEvenOffsetTicks` | Offset added to the break-even stop in ticks (default 300). |
| `MacdFastLength` | Fast EMA period of MACD (default 12). |
| `MacdSlowLength` | Slow EMA period of MACD (default 26). |
| `MacdSignalLength` | Signal EMA period of MACD (default 9). |
| `CandleType` | Trading timeframe candle type. |
| `HigherCandleType` | Confirmation timeframe candle type. |
| `MacdCandleType` | MACD timeframe candle type. |

## Notes

- The strategy expects `Security.PriceStep` to be populated so that tick-based risk controls translate to price distances correctly.
- Only one net position is maintained at a time; opposite signals are ignored until the current position is closed.
- The logic processes only finished candles to avoid acting on partial data.
