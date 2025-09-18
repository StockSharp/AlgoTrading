# Cross MA ATR Notification Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
This strategy is a StockSharp port of the MetaTrader 4 "CrossMA" expert advisor. It trades the crossover between two simple moving averages and protects each trade with an Average True Range (ATR) based stop loss. In addition to the original logic, the strategy records detailed information messages instead of sending e-mails.

## Trading Logic
1. The strategy subscribes to the configured candle series and calculates a fast and a slow simple moving average together with an ATR indicator.
2. When the fast SMA crosses above the slow SMA, any short exposure is closed and a long position is opened. The stop loss is placed one ATR below the entry price.
3. When the fast SMA crosses below the slow SMA, any long exposure is closed and a short position is opened. The stop loss is placed one ATR above the entry price.
4. On every finished candle the stop price is checked. If price touches the stop level the position is immediately closed at market.

## Risk Management
- Position size is computed from the account equity and the `Maximum Risk` parameter. If equity information is not available the strategy falls back to the `Base Volume` value.
- After two or more consecutive losing trades the position size is reduced proportionally to the `Decrease Factor`, reproducing the original MetaTrader behaviour.
- All volumes are normalized to the security volume step to ensure valid order sizes.

## Notifications
Instead of sending e-mails the strategy writes clear log messages whenever orders are opened or closed by signals or stops. Logging can be disabled through the `Enable Notifications` parameter.

## Parameters
- **Candle Type** – candle type used for indicator calculations.
- **Fast SMA Period** – period of the fast moving average (default 4).
- **Slow SMA Period** – period of the slow moving average (default 12).
- **ATR Period** – number of candles used by ATR for the stop calculation (default 6).
- **Base Volume** – minimum traded volume when risk based sizing is unavailable (default 0.1).
- **Maximum Risk** – fraction of equity allocated to each trade (default 0.02).
- **Decrease Factor** – reduces position size after losing trades (default 3).
- **Enable Notifications** – enables logging of trade actions.
