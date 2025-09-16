# Exp TrendValue Strategy

A strategy based on the TrendValue indicator. It builds dynamic support and resistance bands using weighted moving averages of high and low prices shifted by ATR. A new up or down trend is detected when price crosses the opposite band.

## Entry and Exit
- **Long Entry**: When a new upward trend starts.
- **Short Entry**: When a new downward trend starts.
- **Long Exit**: On a downward signal or trend line.
- **Short Exit**: On an upward signal or trend line.

## Parameters
- `BuyPosOpen` / `SellPosOpen` – enable long/short entries.
- `BuyPosClose` / `SellPosClose` – allow closing long/short positions.
- `StopLossPips` – stop loss in price points.
- `TakeProfitPips` – take profit in price points.
- `MaPeriod` – weighted moving average period.
- `ShiftPercent` – percentage shift applied to averages.
- `AtrPeriod` – ATR period.
- `AtrSensitivity` – multiplier applied to ATR.
- `CandleType` – candle timeframe.

## Notes
The strategy subscribes to candle data and updates indicators on each finished candle. Market orders are placed when conditions are met and protective stop and take profit levels are tracked internally.
