# Super Woodies CCI Strategy

This strategy is a conversion of the original MQL5 *Exp_SuperWoodiesCCI* expert advisor. It trades based on the direction of the Commodity Channel Index (CCI) calculated on a higher time frame.

## Logic

- Calculate CCI with a configurable period.
- When CCI crosses above zero:
  - Optionally close short positions.
  - Optionally open a long position.
- When CCI crosses below zero:
  - Optionally close long positions.
  - Optionally open a short position.

Only completed candles are processed, and the strategy operates on a specified candle type.

## Parameters

- **CciPeriod** – period for CCI calculation.
- **CandleType** – time frame of candles to analyse.
- **AllowLongEntry** – enable opening long positions.
- **AllowShortEntry** – enable opening short positions.
- **AllowLongExit** – enable closing long positions when CCI is negative.
- **AllowShortExit** – enable closing short positions when CCI is positive.

## Notes

The strategy uses the high-level StockSharp API with `SubscribeCandles` and indicator binding. Trading methods `BuyMarket` and `SellMarket` are used for position management.
