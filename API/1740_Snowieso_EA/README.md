# Snowieso Strategy

This strategy combines a fast and slow **Linear Weighted Moving Average (LWMA)** with **MACD** and **Kaufman Adaptive Moving Average (KAMA)** to confirm trend direction.

## How It Works
1. Subscribe to candles of the chosen timeframe.
2. Calculate Fast LWMA, Slow LWMA, MACD and KAMA values.
3. **Long entry**: occurs when the fast LWMA crosses above the slow LWMA, the MACD histogram is positive and KAMA is rising.
4. **Short entry**: occurs when the fast LWMA crosses below the slow LWMA, the MACD histogram is negative and KAMA is falling.
5. A fixed stop loss and take profit are applied using `StartProtection`.

The strategy closes opposite positions before opening new ones and visualizes indicators and trades on a chart.

## Parameters
- `FastLength` – period of the fast LWMA.
- `SlowLength` – period of the slow LWMA.
- `MacdFast`, `MacdSlow`, `MacdSignal` – MACD configuration.
- `KamaLength` – lookback period for KAMA.
- `StopLossPoints` – absolute stop loss in price points.
- `TakeProfitPoints` – absolute take profit in price points.
- `CandleType` – timeframe of processed candles.

## Usage
Deploy the strategy on a selected security. The algorithm automatically subscribes to candles and manages positions based on indicator signals. The high-level API is used for data binding and order execution.
