# YTG ADX Level Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports Yuriy Tokman's `_ADX.mq5` expert advisor to the StockSharp high-level API. It monitors the Average Directional Index and reacts when the +DI or -DI components surge through configurable thresholds. Orders are opened only once at a time, mirroring the original MQL logic, and protective stop-loss and take-profit levels expressed in price points are applied automatically.

## Overview

- **Market regime**: Works on trending or strongly directional moves where DI spikes accompany breakouts.
- **Direction**: Opens either long or short positions, but never both simultaneously.
- **Timeframe**: Controlled by the `CandleType` parameter (default 1-hour candles).
- **Data**: Uses finished candles to calculate ADX/DI values from the `AverageDirectionalIndex` indicator.

## Trading Logic

1. Subscribe to the selected candle series and build the ADX indicator with the configured `AdxPeriod`.
2. For each finished candle, collect the +DI and -DI values and keep only the amount of history required by the `Shift` parameter. A `Shift` of 1, identical to the MQL default, evaluates the previous closed candle.
3. **Long entry**: Triggered when the shifted +DI value rises above `LevelPlus` while its previous value was below the same threshold. The strategy checks that no position is currently open before buying at market.
4. **Short entry**: Triggered when the shifted -DI value rises above `LevelMinus` while its previous value was below that level. A market sell is sent only if there is no active position.
5. Exits are handled exclusively by protective orders launched through `StartProtection`: a fixed take-profit and stop-loss measured in price points, equivalent to `TP` and `SL` from the original code.

This implementation intentionally avoids averaging into positions, reentries while trades are active, or additional filters, matching the lightweight behaviour of the source EA.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 1-hour time frame | Time frame of the candle subscription used for ADX calculation. |
| `AdxPeriod` | 28 | Length of the Average Directional Index and its DI calculations. |
| `LevelPlus` | 5 | Threshold that the +DI series must exceed to open a long position. |
| `LevelMinus` | 5 | Threshold that the -DI series must exceed to open a short position. |
| `Shift` | 1 | Number of closed candles to look back when evaluating the DI crossing (1 = previous candle). |
| `TakeProfitPoints` | 500 | Distance in price points for the take-profit order. Multiplied by the instrument's tick size internally. |
| `StopLossPoints` | 500 | Distance in price points for the protective stop-loss order. |
| `TradeVolume` | 0.1 | Base volume for new market orders, matching the `Lots` setting in the MQL expert. |

## Risk Management

- `StartProtection` converts the point-based take-profit and stop-loss values into absolute price distances using the instrument's `PriceStep`.
- No trailing stop or breakeven logic is applied; exits occur solely through the configured protective orders.

## Notes and Tips

- Extremely low DI thresholds may lead to frequent whipsaw trades, while higher levels wait for stronger directional bursts.
- The `Shift` parameter can be increased when you need confirmation from earlier candles, for example on higher time frames to filter intrabar noise.
- Because the strategy trades only one position at a time, manual interference or external trades on the same account should be avoided to prevent conflicts with the internal position tracking.
