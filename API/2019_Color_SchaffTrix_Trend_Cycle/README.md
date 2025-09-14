# Color Schaff TRIX Trend Cycle Strategy

This strategy implements the **Schaff Trend Cycle** oscillator calculated over the TRIX-based MACD. The oscillator identifies cyclical trend shifts and generates trading signals when the cycle crosses predefined levels.

## How it works

1. Two TRIX oscillators with different lengths are calculated to build a MACD series.
2. The MACD values are processed by a double stochastic transformation to produce the Schaff Trend Cycle (STC).
3. A long position is opened when the STC crosses above the high level and a short position is opened when it crosses below the low level.
4. Existing positions are closed when an opposite cross occurs.

## Parameters

- **Fast TRIX** – length of the fast TRIX oscillator.
- **Slow TRIX** – length of the slow TRIX oscillator.
- **Cycle** – period used in stochastic calculations.
- **High Level / Low Level** – upper and lower thresholds for the STC.
- **Stop Loss % / Take Profit %** – risk control parameters expressed in percentage.
- **Buy/Sell Open/Close** – enable or disable corresponding operations.

## Notes

The strategy uses candle data of the selected timeframe (default 4 hours) and executes market orders. Protection is enabled with both stop-loss and take-profit values. All indicator processing is performed using the high-level API with automatic bindings.

Use this strategy for educational purposes and backtest thoroughly before live trading.
