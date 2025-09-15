# XMA Candles Strategy

## Description
The XMA Candles strategy monitors the direction of smoothed candles calculated from the XMA (Exponential Moving Average) of open and close prices. A candle is considered **bullish** when the smoothed open price is below the smoothed close price, and **bearish** when the smoothed open price is above the smoothed close price. The strategy reacts to color changes of these smoothed candles.

- When a new bullish candle appears after a non-bullish one, the strategy closes any short position and opens a long position.
- When a new bearish candle appears after a non-bearish one, the strategy closes any long position and opens a short position.

## Parameters
- `Length` – number of periods for smoothing open and close prices.
- `CandleType` – timeframe of candles used for calculations.
- `BuyPosOpen` – allow opening long positions.
- `SellPosOpen` – allow opening short positions.
- `BuyPosClose` – allow closing long positions when bearish signal appears.
- `SellPosClose` – allow closing short positions when bullish signal appears.
- `StopLoss` – protective stop in percent.
- `TakeProfit` – profit target in percent.

## Trading Rules
1. Wait for each candle of the selected timeframe to finish.
2. Calculate exponential moving averages for open and close prices.
3. Determine candle color:
   - Green (bullish) if smoothed open < smoothed close.
   - Red (bearish) if smoothed open > smoothed close.
4. If color changes to bullish, close shorts and optionally open a long position.
5. If color changes to bearish, close longs and optionally open a short position.
6. Protective stops and targets are managed by built‑in risk controls.

This strategy is a conversion of the original MQL5 expert "Exp_XMACandles".
