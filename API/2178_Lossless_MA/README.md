# Lossless MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades crossovers between a fast and a slow Simple Moving Average (SMA).
It optionally avoids realizing losses by moving losing positions to break-even when the opposite signal appears.

## How It Works

1. **Indicators**
   - Fast SMA
   - Slow SMA
2. **Entries**
   - **Long** when `Fast SMA > Slow SMA` and current direction is not long.
   - **Short** when `Fast SMA < Slow SMA` and current direction is not short.
   - Additional entries are allowed if `Close Losses` is disabled and the number of open deals is below `Max Deals`.
3. **Exits**
   - On an opposite crossover.
   - If `Close Losses` is enabled, the position is closed immediately.
   - If `Close Losses` is disabled and the trade is losing, a limit order is placed at the entry price to exit at break-even.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `FastLength` | Fast SMA period. | `10` |
| `SlowLength` | Slow SMA period. | `30` |
| `MaxDeals` | Maximum number of simultaneous deals. | `5` |
| `CloseLosses` | Close losing trades immediately. | `true` |
| `Volume` | Order volume. | `1` |
| `CandleType` | Candles for calculations. | `1-minute` |

## Notes

The strategy uses market orders for entries and exits. When `CloseLosses` is disabled, it attempts to protect positions by placing a limit order at the entry price instead of closing at a loss.
