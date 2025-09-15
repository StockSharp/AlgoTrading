# Balance Of Power Histogram Strategy

This strategy is an adaptation of the original MetaTrader expert from `MQL/16214`. It uses the **Balance of Power** (BOP) indicator to detect momentum changes in the market.

## Logic

1. The strategy calculates the Balance of Power for each finished candle:
   
   $$BOP = \frac{Close - Open}{High - Low}$$
2. Three consecutive BOP values are compared.
   - When the previous value is lower than the value before it and the current value is higher than the previous one, the BOP turns upward and the strategy enters a long position.
   - When the previous value is higher than the value before it and the current value is lower than the previous one, the BOP turns downward and the strategy enters a short position.
3. Position is changed only after a completed candle to avoid false signals.

## Parameters

- **CandleType** – timeframe of candles used for calculations. The default is four-hour candles.

## Notes

This port focuses on the core behaviour of the original strategy and does not implement the advanced money-management options from the MQL version.

