# SVM Trader Strategy

## Overview

SVM Trader Strategy demonstrates how a combination of classic technical indicators can approximate the behavior of a support vector machine (SVM) model for generating trading signals. The original MQL example trained two separate SVMs for buy and sell decisions. In this StockSharp conversion, we emulate the decision process with a simple scoring system derived from seven indicators:

- **Bears Power** and **Bulls Power** – measure the balance between sellers and buyers.
- **Average True Range (ATR)** – captures current volatility.
- **Momentum** – checks price acceleration.
- **Moving Average Convergence Divergence (MACD)** – identifies trend direction.
- **Stochastic Oscillator** – detects overbought and oversold levels.
- **Force Index** – combines price movement and volume.

Each indicator contributes to a cumulative score. When the score exceeds a threshold, the strategy opens a long position; when the score falls below the opposite threshold, a short position is opened. This setup mirrors the classification step of the original SVM approach while keeping the implementation lightweight and transparent.

## Parameters

| Name | Description |
| ---- | ----------- |
| `CandleType` | Candle timeframe used for calculations. |
| `Volume` | Order volume for new trades. |
| `TakeProfit` | Distance for take-profit in absolute price units. |
| `StopLoss` | Distance for stop-loss in absolute price units. |
| `RiskExposure` | Maximum cumulative position volume allowed. |

## Trading Logic

1. Subscribe to candles of the specified type and bind all indicators using high-level API.
2. For each finished candle, retrieve indicator values from the binding callback.
3. Calculate a score:
   - Bulls Power greater than Bears Power
   - Momentum above zero
   - MACD line above its signal line
   - Stochastic %K above %D
   - Force Index above zero
4. If at least three conditions are true and the current position is non-positive, a market buy order is placed.
5. If two or fewer conditions are true and the current position is non-negative, a market sell order is placed.
6. `StartProtection` applies both stop-loss and take-profit for every opened position.

## Notes

- Indicator periods are fixed to values from the original MQL example (mostly 13 for symmetry and smoothness).
- The scoring system is a simplified proxy for SVM classification and can be replaced with a more advanced model if required.
- `RiskExposure` prevents over-allocation by limiting the total position size.
- The strategy uses tabs for indentation and English comments as required by project conventions.

## Disclaimer

This strategy is provided for educational purposes. It demonstrates indicator binding and basic risk management in StockSharp. Use and modify at your own risk.
