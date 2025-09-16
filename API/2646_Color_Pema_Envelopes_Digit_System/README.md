# Color PEMA Envelopes Digit System
[Русский](README_ru.md) | [中文](README_cn.md)

The **Color PEMA Envelopes Digit System** reproduces the logic of the MetaTrader expert
`Exp_Color_PEMA_Envelopes_Digit_System.mq5`. The strategy evaluates the color codes
produced by the Color PEMA Envelopes indicator: when a candle closes outside of the
upper or lower band the indicator paints a special color, and once price re-enters the
channel a trade is triggered in the direction of the breakout.

## How it works
1. The strategy builds an eight-stage Polynomial EMA (PEMA) using fractional lengths,
   exactly as in the original indicator. The result is rounded to the configured
   precision and shifted by the optional price offset.
2. Upper and lower envelopes are created by applying a percentage deviation around the PEMA value.
3. Each finished candle receives a color code depending on its relationship to the shifted envelopes:
   - `4`/`3`: close above the upper band (bullish/bearish body).
   - `1`/`0`: close below the lower band (bullish/bearish body).
   - `2`: price remains inside the envelope.
4. The strategy reads the color that occurred on the `SignalBar + 1` candle and compares it with
the color of the `SignalBar` candle. This mimics the `CopyBuffer` calls from the expert advisor.
5. When the older color indicates a breakout above the upper band and the more recent color
returns inside the channel, a long entry is allowed (if enabled) and any short position is closed.
   The mirror logic is used for short entries and for closing long positions.
6. Protective stop-loss and take-profit orders are managed through StockSharp's risk module.

## Parameters
- `CandleType` – timeframe used for analysis and trading.
- `TradeVolume` – quantity sent with market orders.
- `EmaLength` – fractional length used by every EMA layer in the PEMA chain.
- `AppliedPrice` – source price (close, open, median, weighted, trend-follow, DeMark, etc.).
- `DeviationPercent` – percentage distance for both envelopes around PEMA.
- `Shift` – number of completed candles used to offset the envelope comparison.
- `PriceShift` – additional absolute shift applied to both envelopes.
- `Digit` – extra precision digits when rounding the PEMA output.
- `SignalBar` – how many closed candles back to read the current color from (the older color is taken one bar further).
- `AllowBuyOpen` / `AllowSellOpen` – enable or disable new long/short entries.
- `AllowBuyClose` / `AllowSellClose` – permit closing long/short positions on opposite signals.
- `StopLossPoints` – protective stop distance in price points (multiplied by `PriceStep`).
- `TakeProfitPoints` – profit target distance in price points.

## Default values
- `CandleType = TimeSpan.FromHours(4).TimeFrame()`
- `TradeVolume = 1m`
- `EmaLength = 50.01m`
- `AppliedPrice = AppliedPrice.Close`
- `DeviationPercent = 0.1m`
- `Shift = 1`
- `PriceShift = 0m`
- `Digit = 2`
- `SignalBar = 1`
- `AllowBuyOpen = true`
- `AllowSellOpen = true`
- `AllowBuyClose = true`
- `AllowSellClose = true`
- `StopLossPoints = 1000m`
- `TakeProfitPoints = 2000m`

## Filters
- **Category**: Breakout / Channel re-entry
- **Direction**: Long & Short
- **Indicators**: Polynomial EMA envelopes
- **Stops**: Yes (point-based stop-loss and take-profit)
- **Timeframe**: Swing (default 4H)
- **Risk**: Moderate – trades only when price returns from an extreme
- **Seasonality**: None
- **Machine Learning**: No
- **Divergence**: No
