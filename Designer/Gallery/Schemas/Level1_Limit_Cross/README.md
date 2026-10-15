# EMA Cross Limit Entries from Level 1
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram follows the EMA(14)/EMA(50) signal from TwoDLimitsStrategy while making order handling visible. A completed five-minute candle samples Level 1, places a limit slightly behind the best quote, and cancels an unfilled order when the averages cross back.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed fast EMA(14) and slow EMA(50); two Crossing blocks detect both directions.
- BestBidPrice and BestAskPrice arrive asynchronously from Level 1 and are latched on each candle before prices are calculated.
- Buy limits sit 0.02% below the best bid and sell limits 0.02% above the best ask, leaving time for Order cancellation to be observed.
- A fill starts a 100-candle N values cooldown. Until it completes, both new entries and the price stream used by Position protection are disabled.
- Position protection activates after the cooldown with a 0.3% stop and 0.6% target.

## Entry and Exit Rules

- **Long entry**: EMA(14) crosses above EMA(50), Position is zero or short, a positive best bid is available, and the cooldown is complete. A buy limit is placed below the sampled bid; its volume is abs(Position) plus the base volume, so one fill can close a short and open a long.
- **Short entry**: EMA(14) crosses below EMA(50), Position is zero or long, a positive best ask is available, and the cooldown is complete. A sell limit is placed above the sampled ask with the same net-reversal volume rule.
- **Exit**: A reverse EMA cross cancels the latest still-active opposite limit. After an entry fill and the 100-candle holdoff, Position protection exits at a 0.3% stop or 0.6% take profit; a reverse filled limit can also reverse the position.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 00:05:00 | Finished-candle interval used by both EMAs, quote sampling, cooldown counting, and protection checks. |
| Fast EMA Length | 14 | Number of five-minute values in the fast exponential moving average. |
| Slow EMA Length | 50 | Number of five-minute values in the slow exponential moving average. |
| Quote Offset | 0.02% | Percentage moved away from the best quote: below bid for buys and above ask for sells. |
| Base Volume | 1 | Position size opened after any opposite exposure has been netted out. |
| Cooldown, candles | 100 | Finished candles ignored after every strategy fill before entries and protection are enabled again. |
| Stop Loss | 0.3% | Percentage distance of the protective stop from the entry fill. |
| Take Profit | 0.6% | Percentage distance of the profit target from the entry fill. |

## Diagram Details

- The C# source enters with market orders; the diagram intentionally uses Level 1 limit orders so registration and cancellation have a visible lifecycle.
- The original 200/400 price-step distances are represented as approximately 0.3%/0.6% for the BTCUSDT replay price, preserving the 1:2 ratio.
- The source checks neither stop nor target during its first 100 bars after a fill. Gating the protection price reproduces that ordering instead of describing protection as continuously active.
- Quote latches keep the asynchronous Level 1 stream aligned with the candle-clocked EMA decision. Price shrinking is disabled because the replay security may not declare a price step.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
