# RSI Levels Strategy

## Overview

The **RSI Levels Strategy** is a direct port of the MetaTrader 5 "RSI Levels" expert advisor. The system watches a single symbol on the selected timeframe and acts when the Relative Strength Index (RSI) crosses configurable overbought and oversold thresholds. The strategy assumes that the market will mean-revert after the RSI enters an extreme zone. When the indicator falls below the oversold level a long position is initiated, and when it rises above the overbought level a short position is opened. Only one position is held at a time and any opposite exposure is closed before a new entry.

## Trading Logic

1. **RSI Calculation** – The RSI is calculated on the working timeframe with a configurable period. The current bar must be finished before signals are evaluated.
2. **Long Entry** – Triggered when the current RSI closes below the oversold level while the previous RSI value was above that level. If a short position exists it is closed immediately; otherwise, a new long trade is opened using risk-based position sizing.
3. **Short Entry** – Triggered when the current RSI closes above the overbought level while the previous RSI value was below that level. Any existing long exposure is closed first, then a new short trade is created.
4. **Stop Loss** – A fixed stop is placed at a configurable distance in symbol points from the entry price. If the stop is set to zero it is disabled.
5. **Take Profit** – A fixed take-profit is placed at a configurable distance in symbol points from the entry price. If the take-profit is zero it is disabled.
6. **Position Management** – Only one position can be open at a time. After a position is closed the internal state is reset so that the next signal starts from a clean slate.

## Position Sizing

Position size is computed from the configured *Risk % per Trade*. The algorithm multiplies the portfolio equity by the risk percentage, then divides the risk capital by the monetary value of the stop distance (stop points × step price). The resulting volume is rounded down to the nearest tradable lot step and constrained by the minimum/maximum volume provided by the security. When the required market information (price step or step price) is missing, the strategy logs a warning and falls back to the minimal available volume.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 1 hour timeframe | Timeframe used for candle subscription and RSI calculation. |
| `RsiPeriod` | 14 | Number of periods for the RSI indicator. |
| `OverboughtLevel` | 70 | RSI threshold that defines the overbought zone. |
| `OversoldLevel` | 30 | RSI threshold that defines the oversold zone. |
| `RiskPercent` | 2 | Percentage of portfolio equity risked on each trade. |
| `StopLossPoints` | 500 | Stop-loss distance expressed in symbol points. Set to zero to disable. |
| `TakeProfitPoints` | 1000 | Take-profit distance expressed in symbol points. Set to zero to disable. |

## Practical Notes

- The strategy requires `PriceStep`, `StepPrice`, `MinVolume`, and `VolumeStep` to be configured on the security for accurate risk sizing. If any of these values are missing, conservative defaults are used and warnings are logged.
- The logic uses `SubscribeCandles` and `Bind` to obtain indicator values without manually pulling data, matching the high-level API guidelines.
- Stops and targets are evaluated on candle data; slippage and gaps can cause executions away from the intended price.
- Because the system reacts only when a candle is finished, it is suitable for timeframes such as M15, H1, or H4. Lower timeframes may require additional filters to reduce noise.

## Usage

1. Attach the strategy to the desired security and portfolio.
2. Adjust the RSI thresholds and risk controls to match the instrument's volatility.
3. Start the strategy and monitor the log for warnings related to missing symbol information.
4. Review trade results and fine-tune the stop and take-profit distances or RSI levels according to performance.

This StockSharp implementation mirrors the original MetaTrader behaviour while exposing the configuration and risk management through standard strategy parameters.
