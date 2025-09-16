# NRTR ATR Stop Strategy

## Overview

The **NRTR ATR Stop Strategy** is a direct conversion of the MetaTrader expert advisor `Exp_NRTR_ATR_STOP_Tm`. The system combines a Non-Repainting Trend Reversal (NRTR) stop with an Average True Range (ATR) filter to determine the dominant trend and to trail protective levels. Trading decisions are generated on the close of the selected timeframe and can be delayed by a configurable number of fully formed bars to mimic the original signal shift.

The strategy is implemented on top of the high-level StockSharp API. All trading logic is driven by candle subscriptions, indicator bindings, and managed order helpers, ensuring compatibility with the Designer, Shell, Runner, and API products.

## Trading Logic

1. **Indicator calculation**
   - ATR is computed on the selected timeframe with the provided period.
   - The ATR value is multiplied by a coefficient to build the NRTR upper and lower levels.
   - Trend direction changes when the previous candle breaks the opposing NRTR level; these events also create arrow signals that can trigger entries.
2. **Signal delay**
   - The `SignalBarDelay` parameter reproduces the `SignalBar` input from MetaTrader. It delays execution by the chosen number of completed candles, allowing the strategy to evaluate historical signals exactly like the source expert.
3. **Entries**
   - A **long** position opens when a bullish NRTR reversal occurs and long entries are enabled.
   - A **short** position opens when a bearish NRTR reversal occurs and short entries are enabled.
4. **Exits**
   - Directional reversals close any opposing position if closing is allowed for that side.
   - An optional session filter can force all positions to be closed outside the allowed trading window.
   - Additional risk management is handled through stop-loss and take-profit distances expressed in price steps. The NRTR level also trails an active position by tightening the protective stop in the direction of the trend.

## Risk Management

- **Volume**: Trades are opened with the configurable `OrderVolume` parameter. Volume can be optimized just like in the MetaTrader version.
- **Stop-loss / take-profit**: Distances are specified in multiples of the security price step, matching the original point-based settings. When both a manual stop and an NRTR level are available, the protective price is chosen conservatively (closest to the market) to avoid widening risk.
- **Session control**: When `UseTradingWindow` is enabled the strategy only opens positions inside the defined `[StartHour:StartMinute, EndHour:EndMinute]` interval and closes any open position as soon as the market leaves that window.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `OrderVolume` | 1 | Volume used when sending market orders. |
| `StopLossPoints` | 1000 | Stop distance in price steps. Set to `0` to disable. |
| `TakeProfitPoints` | 2000 | Take-profit distance in price steps. Set to `0` to disable. |
| `BuyPosOpen` / `SellPosOpen` | `true` | Allow opening long or short positions on NRTR reversals. |
| `BuyPosClose` / `SellPosClose` | `true` | Allow closing long or short positions when an opposite signal appears. |
| `UseTradingWindow` | `true` | Enable the time filter that mimics the original expert advisor. |
| `StartHour` / `StartMinute` | 0 / 0 | Beginning of the allowed trading session. |
| `EndHour` / `EndMinute` | 23 / 59 | End of the allowed trading session. Supports overnight ranges. |
| `CandleType` | 1-hour time frame | Candle type used for the ATR and NRTR calculations. |
| `AtrPeriod` | 20 | Number of bars used to calculate ATR. |
| `AtrMultiplier` | 2 | Coefficient applied to ATR when building NRTR levels. |
| `SignalBarDelay` | 1 | Number of completed bars to delay signal execution. |

## Notes

- The strategy uses candle-level processing only; tick-by-tick replication of the original EA is intentionally avoided to remain consistent with the high-level StockSharp architecture.
- Comments inside the code are provided in English as required by the project guidelines.
- A Python version is intentionally omitted to match the user request.
