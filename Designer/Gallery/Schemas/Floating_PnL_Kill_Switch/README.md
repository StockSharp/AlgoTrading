# Floating PnL Kill Switch Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram wraps a CCI return signal in a money-based emergency layer. Entries are visible as limit orders at the hourly close; once unrealized profit reaches either boundary, the diagram cancels every working order before flattening the position at market.

![schema](schema.svg)

## Strategy Overview

- Finished hourly candles feed a 30-period CommodityChannelIndex and provide the limit price through their close.
- A long signal requires the previous CCI below -100 and the current CCI back at or above -100. The short side mirrors the return from above +100.
- Position must be non-positive for a buy and non-negative for a sell, so an opposite signal can reduce an existing position instead of adding in the same direction.
- A fill resets a four-candle cooldown. The capped counter must reach its limit before either entry gate can pass again.
- Order registering posts the accepted signal as a limit order at the finished candle close with price shrinking disabled.
- P&L change compares unrealized profit with +300 and -200. Either result triggers Mass order cancellation and Modify position with ClosePosition.

## Entry and Exit Rules

- **Long entry**: The previous CCI was below -100, the current CCI is at least -100, Position is not long, and four finished candles have elapsed since the latest fill. A buy limit is posted at the current close.
- **Short entry**: The previous CCI was above +100, the current CCI is at most +100, Position is not short, and the cooldown has elapsed. A sell limit is posted at the current close.
- **Exit**: There is no price-based take profit or stop. When unrealized P&L reaches 300 or falls to -200, the kill switch first requests cancellation of all working strategy orders and simultaneously sends a market ClosePosition action.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 01:00:00 | Time frame of the finished candles used by CCI, the cooldown, and limit prices. |
| CCI Length | 30 | Number of hourly values in CommodityChannelIndex. |
| CCI Level | 100 | Absolute CCI threshold used symmetrically as +Level and -Level. |
| Signal Cooldown, candles | 4 | Number of completed candles required after the latest fill before another signal may trade. |
| Order Volume | 1 | Quantity of each limit entry order. |
| Target Profit, money | 300 | Unrealized account-currency profit that triggers emergency liquidation. |
| Cut Loss, money | -200 | Unrealized account-currency loss boundary; it is normally negative. |

## Diagram Details

- Previous value stores the preceding CCI output, making each entry a return through the threshold rather than a condition repeated throughout an extreme zone.
- The cooldown starts from an actual strategy fill, not from a signal or registration attempt, and its counter is capped at four.
- Limit-at-close entries deliberately expose a pending-order lifecycle. They may remain working, which gives Mass order cancellation meaningful work during liquidation.
- The target and loss values are absolute account-currency amounts from unrealized P&L. They are an emergency overlay around the CCI engine, not percentage price protection.
- Mass cancellation is portfolio-wide for this strategy because no Security or Portfolio filter socket is connected. Modify position then closes whichever side remains open.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
