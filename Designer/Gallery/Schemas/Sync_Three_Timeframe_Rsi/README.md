# Three-Timeframe RSI Agreement Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram waits for formed RSI values from five-, fifteen-, and thirty-minute candles, samples all three at each slow-timeframe close, and evaluates them as one synchronized group. Agreement below 30 enters or reverses long; agreement above 70 enters or reverses short.

![schema](schema.svg)

## Strategy Overview

- Three finished candle streams calculate independent RSI(14) values on five-, fifteen-, and thirty-minute time frames.
- The thirty-minute RSI event samples the latest value from every stream. Sync groups those three samples with a thirty-minute interval and clears them after release.
- A decision is made once per formed thirty-minute RSI value, only after all three synchronized outputs have updated their comparisons.
- All three RSI values below the Buy Threshold produce a long setup. All three above the Sell Threshold produce a short setup.
- Position gates prevent another order in the currently aligned direction. A setup against the held position submits a two-unit market reversal; a flat entry uses one unit.
- There is no independent stop-loss, take-profit, timed exit, or cooldown. The next qualified opposite setup is the only exit and immediately establishes the new direction.

## Entry and Exit Rules

- **Long entry**: Require synchronized Fast RSI, Middle RSI, and Slow RSI values to be strictly below 30 and Position to be less than or equal to zero. Buy `Base Volume + abs(sign(Position))` at market: one unit from flat or two units from the diagram's one-unit short.
- **Short entry**: Require all three synchronized RSI values to be strictly above 70 and Position to be greater than or equal to zero. Sell the same calculated quantity at market: one unit from flat or two units from the diagram's one-unit long.
- **Exit**: A long is closed only by a qualified short setup, and a short only by a qualified long setup. The reversal order both closes the one-unit exposure and opens one unit in the new direction.
- **Position scope**: The normalized formula targets positions created by this diagram. If an external position has an absolute size greater than one base unit, a two-unit reversal order will not necessarily reach the intended target.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast Candles Series | 00:05:00 | Finished five-minute candles used by Fast RSI. |
| Middle Candles Series | 00:15:00 | Finished fifteen-minute candles used by Middle RSI. |
| Slow Candles Series | 00:30:00 | Finished thirty-minute candles that schedule synchronized decisions. |
| Fast RSI Length | 14 | Averaging length of RSI on the fast candle stream. |
| Fast RSI Source | Not set | No alternate indicator input field is selected. |
| Middle RSI Length | 14 | Averaging length of RSI on the middle candle stream. |
| Middle RSI Source | Not set | No alternate indicator input field is selected. |
| Slow RSI Length | 14 | Averaging length of RSI on the slow candle stream. |
| Slow RSI Source | Not set | No alternate indicator input field is selected. |
| Buy Threshold | 30 | Strict upper boundary for agreement that permits a long entry. |
| Sell Threshold | 70 | Strict lower boundary for agreement that permits a short entry. |
| Base Volume | 1 | Target exposure and flat-entry size; reversals use twice this default size. |

## Diagram Details

- Each Candles block emits only finished values and can build its time frame from smaller stored candles. Each RSI block emits only after its own 14-value warm-up is complete.
- Fast and Middle RSI become ready earlier than Slow RSI. Three indicator-value Variables are triggered by each Slow RSI event, so incomplete warm-up intervals cannot remain at the head of the Sync queue.
- Sync has exactly three connected input/output pairs, Interval `00:30:00`, and Clear Sockets enabled. Its outputs carry the latest fast, latest middle, and current slow RSI samples with one common decision time.
- Six Comparison blocks apply strict `< Buy Threshold` and `> Sell Threshold` tests. A Boolean release reaches both five-input AND gates only after all six comparisons have processed the current group.
- Current Position is compared with zero using `<=` for the long route and `>=` for the short route. These checks allow a flat entry or an opposite-position reversal and suppress repeated same-direction entries.
- Formula evaluates `Base Volume + abs(sign(Position))`. With the maintained exposure, the result is 1 when flat and 2 when invested, while remaining bounded even when the position feed is reported more than once by multiple data subscriptions.
- Buy and Sell Modify position blocks use NoCondition market actions because direction and eligibility are already established by the external gates. No protective or chart block is present.
- Thresholds are deliberately separated from RSI lengths and candle series. Changing a time frame or length changes warm-up timing; Sync still waits for a fresh sampled triple before evaluating.

## Usage

Import the `.json` file into Designer, run it in the backtester with enough history to form the thirty-minute RSI, and adjust the three candle series, RSI lengths, thresholds, and base volume for the instrument before live trading.
