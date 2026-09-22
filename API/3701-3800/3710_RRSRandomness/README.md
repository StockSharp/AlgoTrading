# RRS Randomness Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Overview

The **RRS Randomness Strategy** is a StockSharp port of the “RRS Randomness in Nature EA” for MetaTrader 4.  
It emulates the original expert advisor with pseudo-random long or short market entries, finished-candle stop-loss and take-profit checks, optional trailing protection, and risk-based liquidation when floating losses reach the configured threshold.

Because StockSharp uses net positions per security, simultaneous long and short exposure is not supported. The `DoubleSide` mode therefore starts with a long and alternates direction after each entry instead of maintaining two hedged trades as in MetaTrader.

## Trading Logic

1. On every finished candle the strategy uses the candle close for protection checks and the latest Level1 bid/ask, when available, for spread and liquidation-price calculations.
2. If there is an open position it checks stop loss, take profit, trailing stop, and then the floating-loss risk limit; at most one close order is submitted per candle.
3. When flat, it validates spread and volume constraints before opening a new trade:
   - **DoubleSide** mode alternates between long and short entries, starting with long.
   - **OneSide** mode uses a repeatable pseudo-random integer in `[0,5]`: `1` or `4` opens long, `0` or `3` opens short, and `2` or `5` skips the candle. The sequence restarts when the strategy starts or resets.
4. Trade volumes are drawn uniformly between the configured minimum and maximum and are aligned to the instrument volume step.

## Parameters

| Group | Name | Description |
|-------|------|-------------|
| General | `Mode` | Trading mode: alternate entries (`DoubleSide`, `0`) or random gated entries (`OneSide`, `1`). |
| Lot Settings | `MinVolume` / `MaxVolume` | Volume range for randomly generated trades. |
| Protection | `TakeProfitPoints` | Take-profit distance in price steps. |
| Protection | `StopLossPoints` | Stop-loss distance in price steps. |
| Protection | `TrailingStartPoints` | Profit distance that enables trailing stop management. |
| Protection | `TrailingGapPoints` | Offset between market price and trailing stop. |
| Filters | `MaxSpreadPoints` | Maximum allowed Level1 spread in price steps. Zero blocks all new entries; a positive value allows candle-driven entries when bid/ask are unavailable. |
| Filters | `SlippagePoints` | Informational slippage setting (not enforced automatically). |
| Risk Management | `MoneyRiskMode` | Fixed currency loss (`FixedMoney`, `0`) or percent of portfolio value (`BalancePercentage`, `1`). |
| Risk Management | `RiskValue` | Amount of risk (currency or percent depending on the mode). |
| General | `TradeComment` | Comment attached to entries; close orders append their trigger reason. |
| General | `CandleType` | Candle series driving the decision loop. |

## Notes

- Level1 quotes improve spread and liquidation-price calculations. If both sides are unavailable, a positive spread limit permits the candle-driven fallback; `MaxSpreadPoints = 0` always disables entries.
- Protection is evaluated on finished candles. Trailing activates after a gain of `TrailingStartPoints + TrailingGapPoints` steps and then follows price at `TrailingGapPoints`.
- `FixedMoney` interprets `RiskValue` as account currency; `BalancePercentage` uses that percentage of current portfolio value.
