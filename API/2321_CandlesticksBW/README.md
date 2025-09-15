# CandlesticksBW Strategy

This strategy replicates the Bill Williams CandlesticksBW approach. It colors each candle using Awesome Oscillator (AO) and Accelerator Oscillator (AC) momentum. The strategy opens or closes positions based on transitions between bullish and bearish colors.

## How it works
- Computes AO as the difference between 5- and 34-period SMAs of the median price.
- Computes AC as AO minus a 5-period SMA of AO.
- Each candle is classified into six colors depending on AO/AC growth and candle direction.
- A bullish setup occurs when the penultimate candle is bullish (color 0 or 1). If the last candle's color is above 1, a long position is opened and short positions are closed.
- A bearish setup occurs when the penultimate candle is bearish (color 4 or 5). If the last candle's color is below 4, a short position is opened and long positions are closed.
- Stops and targets are applied via `StartProtection`.

## Parameters
- `CandleType` – candle timeframe.
- `SignalBar` – offset bar for signal evaluation.
- `StopLoss` – stop loss distance in points.
- `TakeProfit` – take profit distance in points.
- `BuyPosOpen` – allow opening long positions.
- `SellPosOpen` – allow opening short positions.
- `BuyPosClose` – allow closing long positions.
- `SellPosClose` – allow closing short positions.

## Indicators
- Awesome Oscillator (derived from SMAs).
- Accelerator Oscillator.

## Trading rules
- **Long entry:** penultimate candle color <2 and last color >1.
- **Short entry:** penultimate candle color >3 and last color <4.
- **Long exit:** on short entry condition if position >0.
- **Short exit:** on long entry condition if position <0.
