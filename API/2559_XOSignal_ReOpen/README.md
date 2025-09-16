# XOSignal Re-Open Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the MetaTrader expert *Exp_XOSignal_ReOpen* inside StockSharp using the high-level API. It trades candlestick data of the selected symbol and timeframe with an XO-style breakout detector built on ATR(13). When an up arrow appears the algorithm closes shorts, optionally opens a long, and then adds to the position every time price progresses by a fixed number of ticks. Down arrows behave symmetrically for shorts. Hard stops and targets in ticks are applied to every layer of the pyramid.

## Core Logic

- The strategy computes an XO range channel whose bands expand by `Range * PriceStep`. Breakouts reset the bands and establish the current trend direction.
- ATR(13) controls how far below/above the candle the virtual entry levels (arrows) are plotted: long arrows appear at `Low - ATR * 3/8`, short arrows at `High + ATR * 3/8`.
- Only completed candles are processed. Signals can be delayed by `SignalBar` bars to mimic the original buffering logic.

## Entry Rules

- **Long entry**: when an up arrow is emitted, long entries are allowed (`EnableBuyEntries = true`), no short position is open, and the signal has not been executed yet. The trade volume equals `Volume`.
- **Long re-entry**: while in a long position, every additional `PriceStepTicks` ticks in favour of the trade (based on candle close) triggers another buy until `MaxPyramidingPositions` layers are opened. Each re-entry updates the protective stop/target levels.
- **Short entry / re-entry**: mirror logic of the long side using the down arrow.

## Exit Rules

- **Signal-based exits**: an up arrow closes every active short when `EnableSellExits = true`; a down arrow closes the long when `EnableBuyExits = true`.
- **Risk exits**: every open layer carries the same stop loss and take profit distance defined in ticks (`StopLossTicks`, `TakeProfitTicks`). When price pierces the level within the current candle, the whole position is flattened.
- **Manual flattening**: opposite entry signals also neutralise the previous direction before opening a new position.

## Position Management

- Position size is fixed by `Volume` for each order.
- Stop loss and take profit are measured in security ticks. Setting them to zero disables the corresponding protection.
- The pyramid counter resets to zero after any full exit so that the next signal starts from a fresh base position.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `Volume` | Order size for each entry | `1` |
| `StopLossTicks` | Stop distance in ticks, 0 disables | `1000` |
| `TakeProfitTicks` | Take profit distance in ticks, 0 disables | `2000` |
| `PriceStepTicks` | Minimum favourable move before adding to the position | `300` |
| `MaxPyramidingPositions` | Maximum number of layered entries (including the first) | `10` |
| `EnableBuyEntries` / `EnableSellEntries` | Allow opening long/short positions | `true` |
| `EnableBuyExits` / `EnableSellExits` | Allow closing long/short positions on opposite arrows | `true` |
| `CandleType` | Timeframe used for signals | `H4` |
| `Range` | XO box height in ticks | `10` |
| `AppliedPrice` | Price source used in the XO detector | `Close` |
| `SignalBar` | Number of closed bars to delay signals | `1` |

The strategy is designed for backtesting or live trading with instruments that provide a reliable price step. Adjust the tick-based distances to match the volatility of the selected market.
