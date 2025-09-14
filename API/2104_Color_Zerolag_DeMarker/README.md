# Color Zerolag DeMarker Strategy

This strategy converts the original MQL5 expert `Exp_ColorZerolagDeMarker` to the StockSharp framework. It uses a custom combination of several **DeMarker** indicators to build fast and slow trend lines. Trading signals are generated when these lines cross.

## Indicators

- Five DeMarker indicators with different periods: 8, 21, 34, 55 and 89.
- Each indicator value is multiplied by a weight factor (0.05, 0.10, 0.16, 0.26 and 0.43).
- The weighted values are summed to form the **fast** line.
- The **slow** line is an exponentially smoothed version of the fast line controlled by the `Smoothing` parameter.

## Trading Logic

1. Subscribe to candles with a configurable timeframe.
2. On each finished candle, calculate the fast and slow lines.
3. When the previous fast line is above the previous slow line and the current fast line falls below the slow line:
   - Close short positions if allowed.
   - Open a long position if enabled.
4. When the previous fast line is below the previous slow line and the current fast line rises above the slow line:
   - Close long positions if allowed.
   - Open a short position if enabled.
5. Optional stop-loss and take-profit percentages are applied for newly opened positions.

## Parameters

- `CandleTimeframe` – timeframe for candle subscription.
- `Smoothing` – smoothing factor for the slow line.
- `Factor1`–`Factor5` – weight factors for each DeMarker period.
- `DeMarkerPeriod1`–`DeMarkerPeriod5` – periods for DeMarker indicators.
- `Volume` – order volume.
- `OpenBuy` / `OpenSell` – enable long/short entries.
- `CloseBuy` / `CloseSell` – enable exits for long/short positions.
- `StopLossPct` / `TakeProfitPct` – optional percentage-based risk management.

## Notes

The strategy operates on closed candles only and uses the high-level StockSharp API (`SubscribeCandles` and `Bind`). All comments in the code are provided in English for clarity.
