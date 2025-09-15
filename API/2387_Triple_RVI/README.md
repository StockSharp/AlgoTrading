# Triple RVI Strategy

This strategy trades using the **Relative Vigor Index (RVI)** on three different timeframes. The longer-term RVI trends act as filters, while the shortest timeframe is used for entries. A long position is opened when the short-term RVI crosses below its signal line while both higher timeframes remain bullish. A short position is opened when the short-term RVI crosses above its signal line and both higher timeframes are bearish. Positions are closed when any timeframe indicates a trend change against the current position.

## Parameters
- **RviPeriod** – period for calculating the RVI.
- **CandleType1** – timeframe of the highest RVI filter.
- **CandleType2** – timeframe of the middle RVI filter.
- **CandleType3** – trading timeframe where entry signals are generated.
- **Volume** – order size used for market orders.

## Notes
- Only finished candles are processed.
- The strategy uses the StockSharp high level API.
- Default timeframes correspond to 30, 15 and 5 minute candles.
