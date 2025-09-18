# 3MA Bunny Cross Strategy

## Overview
The **ThreeMaBunnyCrossStrategy** is a conversion of the MetaTrader 4 expert advisor "3MA Bunny Cross". It trades trend reversals based on the crossover between two linear weighted moving averages (LWMAs) calculated on the closing prices of the selected timeframe. The StockSharp version keeps the original idea of reversing the position immediately after a crossover and adds high-level API conveniences such as indicator binding and built-in risk protection.

## Original MQL Description
The source expert advisor uses two LWMAs with periods 5 and 20. When the fast LWMA crosses the slow LWMA, the advisor closes the opposite position if it exists and immediately opens a new trade in the direction of the crossover. Only one position is allowed at any moment. The original script also checks for a minimum number of bars and free margin before trading.

## StockSharp Implementation Details
- The strategy subscribes to candles defined by the `CandleType` parameter (15-minute timeframe by default) and binds them to two `LinearWeightedMovingAverage` indicators.
- Indicator values are provided directly to the processing method through `Bind`, removing the need for manual buffer handling.
- The previous fast and slow values are cached to detect crossovers using the same logic as the MQL version (`fast` crossing above or below `slow`).
- When a bullish crossover occurs and the current position is flat or short, the strategy sends a market buy order sized to both close any short exposure and open a new long (`Volume + |Position|`). The bearish crossover behaves symmetrically for sells.
- `StartProtection()` is called once at start to enable built-in position protection routines.
- Chart visualization draws the source candles along with the two moving averages and the strategy's own trades.

## Parameters
- **CandleType** – data type describing the candle series to subscribe to (defaults to 15-minute time frame).
- **FastPeriod** – period of the fast LWMA. Default: 5. Optimizable.
- **SlowPeriod** – period of the slow LWMA. Default: 20. Optimizable.

## Indicators
- `LinearWeightedMovingAverage` (fast, period 5 by default).
- `LinearWeightedMovingAverage` (slow, period 20 by default).

## Trading Rules
1. Wait for a finished candle and verify that the strategy is formed, online, and allowed to trade.
2. Detect a bullish crossover when the fast LWMA was below or equal to the slow LWMA on the previous candle and is above or equal to it on the current candle. In this case, close any existing short position and open a long.
3. Detect a bearish crossover when the fast LWMA was above or equal to the slow LWMA on the previous candle and is below or equal to it on the current candle. In this case, close any existing long position and open a short.
4. Each new order size is calculated as `Volume + |Position|` to fully reverse any outstanding exposure, ensuring that only one directional position exists at a time.
