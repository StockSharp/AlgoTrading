# Two Assets with Their Own Averages Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram aligns finished fifteen-minute BTCUSDT@BNBFT and TONUSDT@BNBFT candles, compares each close with a 20-period simple moving average calculated for that instrument, and uses the opposing relationships to manage long and short BTCUSDT exposure. A signed fill-driven latch, one-step market reversals, ordinary exits, and a local fixed 2% stop complete the workflow.

![schema](schema.svg)

## Strategy Overview

- Separate security variables configure only the BTCUSDT and TONUSDT candle subscriptions. Order actions and the strategy-trades stream use the selected Strategy Security, which must be set to BTCUSDT@BNBFT to match the Traded Security parameter.
- Only finished fifteen-minute candles enter a Sync block. Each aligned pair feeds BTC Close and BTC SMA(20) on one leg, and TON Close and TON SMA(20) on the other; decisions begin only after both averages are formed.
- The long relation is strictly `BTC Close < BTC SMA(20)` together with `TON Close > TON SMA(20)`. The short relation is strictly `BTC Close > BTC SMA(20)` together with `TON Close < TON SMA(20)`. Equality does not satisfy either complete relation.
- A numeric signed latch records the diagram-managed BTC state: `-1` means short, `0` means flat, and `1` means long. From flat, a complete relation submits a one-unit market entry. From the opposite state, the action volume becomes two units, closing the existing one-unit exposure and establishing one unit in the new direction with one market action.
- A complete opposite relation has priority over an ordinary exit. Otherwise, BTC being strictly on the exit side of its own average closes the current side with a one-unit ReduceOnly market action. Every synchronized decision finishes before the stored BTC close is released to the local fixed 2% market stop.

## Entry and Exit Rules

- **Long entry**: When a synchronized finished pair has `BTC Close < BTC SMA(20)` and `TON Close > TON SMA(20)`, the buy gate accepts a flat or short latch. It submits a NoCondition market buy for Volume 1 from flat, or Volume 2 from a one-unit short to reverse directly into a one-unit BTC long.
- **Short entry**: When a synchronized finished pair has `BTC Close > BTC SMA(20)` and `TON Close < TON SMA(20)`, the sell gate accepts a flat or long latch. It submits a NoCondition market sell for Volume 1 from flat, or Volume 2 from a one-unit long to reverse directly into a one-unit BTC short.
- **Exit**: A long closes with a one-unit ReduceOnly market sell when BTC Close is strictly above BTC SMA(20) and the complete short relation is absent. A short closes with a one-unit ReduceOnly market buy when BTC Close is strictly below BTC SMA(20) and the complete long relation is absent. Those absence checks leave a complete opposite relation to the two-unit reversal branch. Local protection can also close either side with a fixed 2% market stop; Take Profit 0 disables the profit target.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Traded Security | BTCUSDT@BNBFT | Security used only by the traded-instrument candle subscription. Set Strategy Security to the same BTCUSDT@BNBFT value because every order action and the strategy-trades stream use Strategy Security. |
| Signal Security | TONUSDT@BNBFT | Security used only by the second candle subscription. Its price-to-average relation contributes to decisions, but no order action is addressed to this variable. |
| BTC Candles Series | 00:15:00 | Finished fifteen-minute BTCUSDT candle series used for synchronization, BTC Close, BTC SMA(20), protection checks, and the chart. |
| TON Candles Series | 00:15:00 | Finished fifteen-minute TONUSDT candle series used for synchronization, TON Close, TON SMA(20), and the chart. |
| BTC SMA Length | 20 | Period of the SimpleMovingAverage calculated from synchronized finished BTCUSDT candles. |
| TON SMA Length | 20 | Period of the SimpleMovingAverage calculated from synchronized finished TONUSDT candles. |
| Base Volume | 1 | Default market-entry quantity. Action volume is `Base Volume * (1 + abs(latch))`, so a flat entry uses Base Volume and a reversal uses twice Base Volume; with the default, these are Volume 1 and Volume 2. |
| Take Profit | 0 | An absolute value of zero disables take-profit protection. |
| Stop Loss | 2% | Adverse percentage distance from the protected fill price that activates the stop-loss. |
| Trailing Stop Loss | false | Disabled, so the 2% stop remains fixed instead of following favorable price movement. |
| Use Market Orders | true | Enabled, so an activated stop closes the protected exposure with a market order. |

## Diagram Details

- Two security [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) blocks feed only their respective [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) blocks. A [Sync](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/sync.html) block aligns the finished streams at `00:15:00` before either leg reaches the decision chain.
- Each synchronized candle is split into Close and a formed [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) value. Four strict comparison blocks construct the opposing long and short relations from each instrument's Close and its own SMA(20).
- A numeric Unit variable holds the signed latch. Action fills write `1` after a buy, `-1` after a sell, and `0` after an ordinary or protective exit. State comparisons permit entries from flat and reversals from the opposite side; the volume formula is `Base Volume * (1 + abs(latch))`.
- The long and short relation gates drive NoCondition, MarketOrder [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) actions. Separate ReduceOnly market actions handle ordinary exits. Each ordinary-exit gate also requires the full opposite relation to be false, so a reversal and a one-unit close cannot be requested for the same synchronized pair.
- [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) receives fills from entries, reversals, and ordinary exits. Take Profit is `0`, Stop Loss is `2%`, Trailing Stop Loss is `false`, Use Market Orders is `true`, and the protection runs locally. The synchronized BTC close is stored first and released to protection only after both signal branches have completed their decision for that pair.
- The chart receives the synchronized BTCUSDT and TONUSDT candle streams, BTC SMA(20), TON SMA(20), the stop-loss order stream, and all BTCUSDT fills from Strategy trades.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
