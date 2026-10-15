# Close on Money Target Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram demonstrates a money-based emergency exit around the SMA(10)/SMA(30) direction logic. Entries are deliberately pending limits, so reaching an unrealized profit or loss boundary gives Mass order cancellation real work before ClosePosition flattens exposure.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed fast and slow simple moving averages; Greater and Less evaluate their state on every candle rather than waiting for a crossing event.
- Bullish state with Position <= 0 submits a buy limit at the close; bearish state with Position >= 0 submits a sell limit.
- Volume is abs(Position) plus base volume, preserving the source's close-then-open reversal as one net order.
- P&L change compares unrealized strategy result with +300 and -150 in account currency.
- Either boundary simultaneously requests Mass order cancellation and a market ClosePosition.

## Entry and Exit Rules

- **Long entry**: Fast SMA is above slow SMA and Position is flat or short. A buy limit at the finished close uses enough volume to cover a short and leave one base unit long.
- **Short entry**: Fast SMA is below slow SMA and Position is flat or long. A sell limit at the finished close uses enough volume to cover a long and leave one base unit short.
- **Exit**: Unrealized P&L >= 300 or <= -150 cancels all active strategy orders and closes the current position at market. Once P&L returns to zero, the threshold signal clears and the strategy may trade again.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 00:05:00 | Finished-candle interval used by both simple moving averages and entry decisions. |
| Fast SMA Length | 10 | Number of values in the fast SimpleMovingAverage. |
| Slow SMA Length | 30 | Number of values in the slow SimpleMovingAverage. |
| Base Volume | 1 | Position size retained after a flat entry or net reversal. |
| Profit Target, money | 300 | Unrealized strategy profit in account currency that triggers liquidation. |
| Loss Limit, money | -150 | Unrealized strategy loss boundary in account currency; keep it negative. |

## Diagram Details

- The C# RequestCloseAll method is never called; its live path only trades SMA state with market orders. This diagram implements the strategy's advertised money-exit intent explicitly.
- The source parameters are portfolio equity levels and default to zero, which is not useful. The diagram instead uses strategy PnLUnreal with replay-oriented values +300 and -150.
- Limit-at-close entries replace the source's market orders so working orders exist for Mass order cancellation. Price shrinking is disabled for replay securities that do not declare a price step.
- The source would call Stop after liquidation. The diagram intentionally remains active, allowing the monthly backtest to demonstrate repeated entry and exit cycles.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
