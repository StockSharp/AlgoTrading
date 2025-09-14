# Color Trend CF Strategy

This strategy is a conversion of the MQL expert **Exp_ColorTrend_CF**. It uses two exponential moving averages to detect trend changes. The fast EMA reacts quickly to price movement, while the slow EMA acts as a trend filter. A long position is opened when the fast EMA crosses above the slow EMA. A short position is opened when the fast EMA crosses below the slow EMA.

## Parameters

- `Period` – base period for the fast EMA; the slow EMA uses double this value.
- `StopLoss` – stop-loss distance in price units.
- `TakeProfit` – take-profit distance in price units.
- `AllowBuyOpen` – permission to open long positions.
- `AllowSellOpen` – permission to open short positions.
- `AllowBuyClose` – permission to close long positions on sell signal.
- `AllowSellClose` – permission to close short positions on buy signal.
- `CandleType` – timeframe for indicator calculation.

## Trading Logic

1. Subscribe to candles of the selected timeframe.
2. Calculate fast and slow EMAs.
3. When fast EMA crosses above slow EMA:
   - Close short positions if allowed.
   - Open long position if allowed.
4. When fast EMA crosses below slow EMA:
   - Close long positions if allowed.
   - Open short position if allowed.
5. For open positions apply stop-loss and take-profit levels.

This implementation uses StockSharp high level API with indicator binding.
