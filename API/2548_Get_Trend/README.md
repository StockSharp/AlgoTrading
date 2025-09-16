# Get Trend Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader expert advisor **"Get trend"**, originally designed for M15 trading with an H1 confirmation filter. The algorithm combines smoothed moving averages and a stochastic oscillator to time mean-reversion entries that align with a higher time-frame trend.

## Trading Logic

- **Primary timeframe:** 15-minute candles are used for signal generation and order execution.
- **Confirmation timeframe:** Hourly candles supply the higher time-frame smoothed moving average and closing price used to validate the prevailing trend.
- **Trend filter:** Both the M15 and H1 closes must be on the same side of their respective smoothed moving averages. Additionally, the M15 close must stay within a configurable distance from its moving average to ensure a pullback entry.
- **Momentum trigger:** Long trades require the stochastic %K line to cross above %D in the oversold region (below 20). Short trades require the inverse crossover in the overbought region (above 80).
- **Order management:** Positions are protected with fixed stop-loss and take-profit levels defined in price points. An optional trailing stop tightens the exit once price advances far enough in the trade's favor.

## Entry Conditions

### Long Setup
1. M15 close is below the M15 smoothed moving average.
2. H1 close is below the H1 smoothed moving average.
3. Distance between the M15 close and M15 average does not exceed the **Price Threshold** (expressed in points/ticks).
4. Stochastic %K and %D are both below 20.
5. The previous %K value was below %D, and the current %K value crossed above %D.
6. No existing long position (a short position will be closed and flipped).

### Short Setup
1. M15 close is above the M15 smoothed moving average.
2. H1 close is above the H1 smoothed moving average.
3. Distance between the M15 close and M15 average does not exceed the **Price Threshold**.
4. Stochastic %K and %D are both above 80.
5. The previous %K value was above %D, and the current %K value crossed below %D.
6. No existing short position (a long position will be closed and flipped).

## Exit Rules

- **Stop-loss:** Set in absolute price points from the entry price.
- **Take-profit:** Set in absolute price points from the entry price.
- **Trailing stop:** When enabled, once price moves beyond the trailing distance, the stop is pulled closer to lock in profits while respecting the configured trailing offset.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `M15CandleType` | Candle type used for signal generation. | 15-minute time-frame |
| `H1CandleType` | Candle type used for confirmation. | 1-hour time-frame |
| `MaM15Length` | Length of the smoothed MA on M15 candles. | 99 |
| `MaH1Length` | Length of the smoothed MA on H1 candles. | 184 |
| `StochasticLength` | %K period of the stochastic oscillator. | 27 |
| `StochasticSignalLength` | %D smoothing period. | 3 |
| `ThresholdPoints` | Maximum distance (in points) between price and the M15 MA to permit entries. | 10 |
| `TakeProfitPoints` | Take-profit distance (in points). | 540 |
| `StopLossPoints` | Stop-loss distance (in points). | 90 |
| `TrailingStopPoints` | Trailing stop distance (in points). | 20 |
| `TradeVolume` | Base order volume used when opening new trades. | 0.1 |

All point-based parameters are multiplied by the instrument's `PriceStep` to convert them to absolute price increments.

## Implementation Notes

- The strategy uses StockSharp's high-level API with candle subscriptions and indicator binding (`BindEx`) to avoid manual buffer management.
- Trailing stop logic mirrors the MetaTrader version: it activates once unrealized profit exceeds the trailing distance and continually tightens the stop toward price.
- Active orders are cancelled before flipping positions to prevent conflicting orders on the book.
- Chart areas display M15 candles with the smoothed moving average and a dedicated stochastic panel for visual diagnostics.

## Usage Tips

- Configure the candle types to match the data provider (e.g., volume-based candles can be substituted if they expose the same DataType concept).
- Adjust the threshold and stop parameters when trading assets with different volatility or tick sizes.
- For best results, apply the strategy to trending instruments where pullbacks toward the moving average are common.
