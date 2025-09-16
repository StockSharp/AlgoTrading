# Derivative Zero Cross Strategy

This strategy trades based on the sign change of the price derivative. The derivative is calculated as the price momentum divided by the period and multiplied by 100. When the derivative crosses the zero line the current position is closed and the opposite position is opened.

## Parameters

- `DerivativePeriod` - smoothing period for the derivative calculation.
- `PriceType` - source price used for the derivative.
- `BuyEntry` - allow opening long positions.
- `SellEntry` - allow opening short positions.
- `BuyExit` - allow closing long positions.
- `SellExit` - allow closing short positions.
- `StopLoss` - stop loss in points.
- `TakeProfit` - take profit in points.
- `CandleType` - candle time frame.

## Logic

1. Subscribe to candles and calculate momentum of the selected price.
2. Derivative is obtained by dividing momentum by the period and scaling by 100.
3. When the derivative turns from positive to non-positive, a long position is opened and short one is closed.
4. When the derivative turns from negative to non-negative, a short position is opened and long one is closed.
5. Stop loss and take profit protection is applied to manage risk.
