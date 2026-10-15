# Single-Instrument SMMA Bias Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Despite the legacy gallery name, this diagram is not a basket. It follows the current VectorStrategy code: one strategy security, fast and slow smoothed moving averages on four-hour candles, net position reversals, and an absolute-money floating P&L exit.

![schema](schema.svg)

## Strategy Overview

- Finished four-hour candles feed SMMA(3) and SMMA(7); their relative order defines bullish or bearish bias.
- A one-shot Flag arms N values, which withholds trading through the first eight completed candles.
- Bullish bias may buy when Position <= 0; bearish bias may sell when Position >= 0.
- Order volume is abs(Position) plus base volume, combining the source's close-and-open pair into one net reversal.
- P&L change closes exposure when unrealized result reaches +5000 or -300000 in account currency.

## Entry and Exit Rules

- **Long entry**: After warmup, fast SMMA is above slow SMMA and Position is flat or short. Modify position sends a market buy sized to close any short and leave one base unit long.
- **Short entry**: After warmup, fast SMMA is below slow SMMA and Position is flat or long. Modify position sends a market sell sized to close any long and leave one base unit short.
- **Exit**: An opposite bias reverses the position directly. Independently, unrealized P&L at or above 5000 or at or below -300000 triggers a market ClosePosition; if the same trend persists, the next eligible candle may enter again.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 04:00:00 | Finished-candle interval used by both smoothed moving averages and position decisions. |
| Fast SMMA Length | 3 | Number of values in the fast SmoothedMovingAverage. |
| Slow SMMA Length | 7 | Number of values in the slow SmoothedMovingAverage. |
| MA Shift Warmup | 8 | Initial finished candles withheld before trend entries are enabled. |
| Base Volume | 1 | Position size retained after a flat entry or net reversal. |
| Profit Target, money | 5000 | Unrealized account-currency profit that triggers ClosePosition. |
| Loss Limit, money | -300000 | Unrealized account-currency loss boundary; keep it negative. |

## Diagram Details

- The source's GetWorkingSecurities yields only (Security, CandleType). Index, Sync, a second security, and basket confirmation are therefore intentionally absent.
- ProfitPercent 0.5 and LossPercent 30 are converted using the test portfolio's 1,000,000 starting balance: +5000 and -300000.
- Designer exposes unrealized P&L in money but not the starting balance, so the percentage thresholds become explicit account-currency values.
- The warmup counts the first eight finished candles, matching the source's processed-bar guard; SMMA(7) is formed before trading begins.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
