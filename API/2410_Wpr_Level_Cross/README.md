# WPR Level Cross Strategy

This strategy trades based on the Williams %R oscillator crossing predefined overbought and oversold levels.

When the indicator crosses below the **Low Level**, it signals a potential reversal from an oversold condition. When it crosses above the **High Level**, it indicates a possible reversal from an overbought condition. Depending on the selected **Trend Mode**, the strategy can trade in the direction of the indicator or invert the signals for counter-trend trading.

## Parameters

- `WprPeriod` – lookback period for Williams %R.
- `HighLevel` – overbought threshold.
- `LowLevel` – oversold threshold.
- `Trend` – `Direct` trades with indicator signals, `Against` inverts them.
- `EnableBuyEntry` / `EnableSellEntry` – allow entering long/short positions.
- `EnableBuyExit` / `EnableSellExit` – allow closing short/long positions.
- `StopLoss` – stop-loss value in price units.
- `TakeProfit` – take-profit value in price units.
- `CandleType` – timeframe of candles used for calculations.

## How It Works

1. The strategy subscribes to candles and calculates the Williams %R indicator.
2. On each finished candle it checks if the indicator crossed the specified levels.
3. Depending on `Trend` and enabled actions it opens or closes positions using market orders.
4. Optional stop-loss and take-profit protection is enabled through `StartProtection`.

## Notes

- Comments in code are provided in English.
- Only the C# version is implemented; Python version is intentionally omitted.
