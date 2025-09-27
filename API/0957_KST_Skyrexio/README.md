# KST Strategy Skyrexio

The strategy goes long when the Know Sure Thing (KST) indicator crosses above its signal while price trades above a chosen moving average and the Alligator jaw. A choppiness index filter can disable entries in ranging markets. Positions are closed using ATR-based stop-loss and take-profit levels.

- **Entry Criteria**: KST crosses above signal, price above filter MA and Alligator jaw, choppiness below threshold.
- **Exit Criteria**: Price hits ATR stop-loss or ATR take-profit.
- **Indicators**: KST, ATR, Moving Average, Alligator jaw, Choppiness Index.

## Parameters
- `CandleType` – candle timeframe.
- `AtrStopLoss` – ATR multiplier for stop-loss.
- `AtrTakeProfit` – ATR multiplier for take-profit.
- `FilterMaType` – type of trend filter MA.
- `FilterMaLength` – length of trend filter MA.
- `EnableChopFilter` – enable choppiness filter.
- `ChopThreshold` – choppiness index threshold.
- `ChopLength` – choppiness index period.
- `RocLen1..4` – ROC lengths for KST.
- `SmaLen1..4` – SMA lengths for KST.
- `SignalLength` – KST signal SMA length.
