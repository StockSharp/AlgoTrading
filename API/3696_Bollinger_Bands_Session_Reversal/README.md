# Bollinger Bands Session Reversal

This strategy is a C# port of the MetaTrader expert advisor **BollingerBandsEA (ver. 3.0)**. It trades mean-reversion setups that occur after price stretches beyond the Bollinger Bands during the active trading session.

## Trading logic

1. Subscribe to the primary intraday candle series (15-minute candles by default) and to a daily candle series used to build the trend filter.
2. Calculate Bollinger Bands (length 20, width 2.0) on the intraday series and a 100-period SMA on daily closes.
3. Track the current and previous day highs/lows, and keep the previous Bollinger Band values for signal evaluation.
4. Only allow entries within the trading session window: from `SessionStartOffsetMinutes` after the trading day open until `SessionEndOffsetMinutes` before the trading day end.
5. Skip trading once the cumulative PnL for the current day turns positive, mimicking the EA daily stop.
6. Enter short when the previous candle is bearish, closed above the upper band, the current close remains above that band, the band width is wide enough, price is below the daily SMA, and price trades above the current or previous daily high.
7. Enter long when the previous candle is bullish, closed below the lower band, the current close remains below that band, the band width is wide enough, price is above the daily SMA, and price trades below the current or previous daily low.
8. Position size is determined by either the configured fixed volume or risk-based sizing that uses the distance to the stop-loss in points.
9. Exits are performed by checking stop-loss, take-profit, optional closing on the middle band, an optional trailing stop, and the optional break-even logic. Losing trades can also be liquidated after a configurable holding time.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Intraday candle series used for trading. |
| `BollingerLength` | Period of the Bollinger Bands moving average. |
| `BollingerWidth` | Width multiplier of the Bollinger Bands. |
| `DailyMaLength` | Length of the daily SMA filter. |
| `StopLossPoints` | Stop-loss distance expressed in instrument points. |
| `UseRiskVolume` | Enables risk-based position sizing. |
| `RiskPercent` | Account percentage used for risk-based sizing. |
| `FixedVolume` | Fallback fixed volume when risk sizing is disabled or not possible. |
| `SessionStartOffsetMinutes` | Minutes after session start before entries are allowed. |
| `SessionEndOffsetMinutes` | Minutes before session end when entries are blocked. |
| `CloseOnMiddleBand` | Exit position when price crosses the Bollinger middle band. |
| `EnableTrailing` | Enables trailing stop adjustments. |
| `TrailingFactor` | Distance multiplier required before trailing the stop. |
| `EnableBreakEven` | Enables moving the stop to the entry price. |
| `BreakEvenFactor` | Profit multiple required to move the stop to break-even. |
| `CloseLosingAfterMinutes` | Closes losing trades after holding them for the specified minutes. |

## Notes

- Protective stop-loss and take-profit orders are simulated by checking candle extrema on every update. Adjust this section if exchange-side protective orders are required.
- Risk-based sizing depends on `Security.Step` and `Security.StepPrice`. If these values are missing, the strategy will fall back to the fixed volume.
- The daily profit stop uses the strategy PnL, therefore realized and floating PnL need to be in the same currency as the portfolio.
