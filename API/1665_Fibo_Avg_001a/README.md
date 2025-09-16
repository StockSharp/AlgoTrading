# Fibo Average MA Crossover Strategy

## Overview
This strategy converts the MetaTrader expert advisor **EA_Fibo_Avg_001a** into the StockSharp framework.
It uses two smoothed moving averages. The slow average length is the sum of the base period and a Fibonacci-based offset.
A long position is opened when the fast average crosses above the slow average, while a short position is opened on the opposite crossover.
Positions are managed with stop loss, take profit and a trailing stop. Optional money management can calculate the order volume from the portfolio size.

## Parameters
- `CandleType` – candle data type.
- `FiboNumPeriod` – additional length added to the slow moving average.
- `MaPeriod` – base period of the moving averages.
- `TrailingStop` – trailing distance in price steps.
- `TakeProfit` – take profit distance in price steps.
- `StopLoss` – stop loss distance in price steps.
- `UseMoneyManagement` – enable simple money management.
- `PercentMm` – portfolio percentage used when money management is enabled.
- `LotSize` – default order volume when money management is disabled.

## Logic
1. Subscribe to candles and calculate two smoothed moving averages.
2. When the fast average crosses above the slow average, buy. When it crosses below, sell.
3. After entering a position set stop loss, take profit and trailing levels.
4. Update trailing stop as price moves in favor and close positions when protective levels are hit or the opposite crossover occurs.
