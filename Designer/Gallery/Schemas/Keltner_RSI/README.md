# Keltner RSI Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A mean-reversion diagram built around the centre line of a Keltner channel. Price stretched below the EMA together with a weak RSI is bought, price stretched above it together with a strong RSI is sold, and the trade is given back when price crosses the average again with RSI past its midpoint. The original strategy computes the ATR channel bands but never reads them, so this diagram leaves them out and keeps only what actually decides a trade.

![schema](schema.svg)

## Strategy Overview

- The 20-period ExponentialMovingAverage is the centre line of the Keltner channel and the only price reference in the whole diagram.
- RSI over 14 candles supplies the second opinion: a reading under 45 confirms the sell-off that is bought, a reading over 55 confirms the push that is sold.
- Both entries need a flat book, and both exits are position-closing blocks, so the four branches can never fight over the same position.
- Two simplifications against the original: the unused ATR bands are dropped, and the 120-bar cooldown that follows every fill has no counter block, so this diagram trades more often.

## Entry and Exit Rules

- **Long entry**: The close is below the EMA, RSI is below the long entry level and the position is flat. The order buys the shared volume at market and opens the long.
- **Short entry**: The close is above the EMA, RSI is above the short entry level and the position is flat. The order sells the shared volume at market and opens the short.
- **Exit**: A long is closed when the close is back above the EMA and RSI is above its midpoint; a short is closed when the close is back below the EMA and RSI is below the midpoint. There is no stop loss and no take profit, exactly as in the original code, where the declared stop-loss percentage is never applied.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| EMA Length | 20 | Length of the ExponentialMovingAverage that serves as the channel centre line. |
| RSI Length | 14 | Averaging length of the RelativeStrengthIndex. |
| RSI Long Entry | 45 | RSI must be under this level for a long entry. |
| RSI Short Entry | 55 | RSI must be over this level for a short entry. |
| RSI Exit Level | 50 | Midpoint RSI has to pass for a position to be closed. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the EMA, the RSI and a converter that reads the closing price.
- Two comparison blocks put the close against the EMA and four more test RSI against its three levels; the position block is compared against a zero constant.
- Two logical ANDs build the entries out of a price condition, an RSI condition and the flat-position check, and drive position modify blocks set to open a position.
- Two more logical ANDs build the exits and drive position modify blocks set to close a position, which need no volume and act only on the side they can actually close.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
