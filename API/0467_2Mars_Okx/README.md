# 2Mars OKX Strategy

This strategy combines a moving-average crossover with a SuperTrend filter. Bollinger Bands provide profit targets while an ATR-based stop loss limits risk.

## Rules
- **Long**: Signal EMA crosses above basis EMA and price is above SuperTrend.
- **Short**: Signal EMA crosses below basis EMA and price is below SuperTrend.
- **Exit**: Take profit at Bollinger upper or lower band, or stop loss at ATR multiplied by a factor.

## Indicators
- EMA
- SuperTrend
- Bollinger Bands
- Average True Range
