# Gonna Scalp Strategy

## Overview

The Gonna Scalp strategy is a high-frequency MetaTrader expert advisor ported to the StockSharp high level API. The system hunts for rapid mean-reversion entries on a short-term chart while respecting the dominant market trend. Confirmation is produced by a voting mechanism that evaluates momentum, CCI, ATR, stochastic oscillator and MACD filters before permitting a trade. Only one position may be open at a time and every trade is protected by fixed stop-loss and take-profit distances expressed in MetaTrader points.

## Trading Logic

1. **Indicator preparation**
   - Fast and slow weighted moving averages (WMA) computed on typical price.
   - Momentum (period 14) evaluated on the trading timeframe and converted into an absolute distance from the neutral value 100.
   - Commodity Channel Index (period 20) and Average True Range (period 12) used as directional filters.
   - Stochastic oscillator %K/%D (5/3/3) and MACD (12/26/9) processed on the same candle series.
2. **Signal voting**
   - Each indicator contributes one vote for the bullish or bearish side when its current reading supports the trend identified in the original MetaTrader code.
   - The strategy collects three recent momentum distances and requires at least one of them to exceed a configurable threshold before allowing a new trade.
   - Additional structure checks demand that the low of the bar two candles ago remains below the high of the previous bar for longs (mirror condition for shorts).
3. **Order execution**
   - When the bullish votes exceed bearish votes and all filters agree, the strategy opens a long position using the configured lot size.
   - When the bearish votes dominate the bullish votes and the momentum filter approves, a short position is opened.
4. **Risk management**
   - Each open trade is accompanied by fixed stop-loss and take-profit distances measured in MetaTrader points and translated into instrument price steps.
   - Protective logic closes the position on the current candle once either level has been breached.

## Key Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `TradeVolume` | Base order size in lots after volume alignment. | `0.01` |
| `FastMaPeriod` | Length of the fast WMA filter. | `1` |
| `SlowMaPeriod` | Length of the slow WMA filter. | `5` |
| `MomentumPeriod` | Number of bars used by the momentum indicator. | `14` |
| `MomentumBuyThreshold` | Minimum absolute momentum deviation required for long entries. | `0.3` |
| `MomentumSellThreshold` | Minimum absolute momentum deviation required for short entries. | `0.3` |
| `StopLossSteps` | Stop-loss distance expressed in MetaTrader points. | `200` |
| `TakeProfitSteps` | Take-profit distance expressed in MetaTrader points. | `200` |
| `CandleType` | Timeframe used for all indicators (defaults to 5-minute candles). | `M5` |

## Usage Notes

- Align the strategy volume with the traded instrument by adjusting `TradeVolume`; the implementation automatically normalizes it to the exchange lot step.
- The stop-loss and take-profit parameters operate in MetaTrader points. They are converted to instrument price units based on the instrument precision.
- At least three completed candles are required before the voting logic can produce signals because of the momentum history buffer.
- The strategy deliberately avoids pyramiding; a new trade is not opened until the previous position has been closed by risk management or an opposite signal.
- You can connect the strategy to StockSharp charts to visualize the WMAs, stochastic and MACD series for signal validation.
