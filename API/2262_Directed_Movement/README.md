# Directed Movement Strategy

## Overview

This strategy replicates the **Directed Movement** expert advisor from MetaTrader. It applies a Relative Strength Index (RSI) that is smoothed twice by moving averages. The first smoothing forms a fast line while the second smoothing creates a slower line.

Trading decisions are based on the crossover of the fast and slow lines in a contrarian fashion:

- **Buy** when the fast line crosses below the slow line.
- **Sell** when the fast line crosses above the slow line.

Optional stop loss and take profit levels are applied as percentages of the entry price.

## Indicators

- `RelativeStrengthIndex` – base momentum indicator.
- `MovingAverage` – first smoothing of the RSI (fast line).
- `MovingAverage` – second smoothing of the fast line (slow line).

## Trading Rules

1. Calculate RSI from candle closes.
2. Smooth the RSI with the first moving average to obtain the fast line.
3. Smooth the fast line with the second moving average to obtain the slow line.
4. Enter a long position when the fast line crosses below the slow line. Close any short position before opening the new long.
5. Enter a short position when the fast line crosses above the slow line. Close any long position before opening the new short.
6. Apply stop loss and take profit protections if their parameters are greater than zero.

## Parameters

| Name | Description |
|------|-------------|
| `CandleType` | Candle series used for calculations. |
| `RsiPeriod` | RSI calculation period. |
| `FirstMaType` | Moving average type used for the fast line. |
| `FirstMaLength` | Period of the fast moving average. |
| `SecondMaType` | Moving average type used for the slow line. |
| `SecondMaLength` | Period of the slow moving average. |
| `StopLossPercent` | Stop loss in percent of entry price. |
| `TakeProfitPercent` | Take profit in percent of entry price. |

