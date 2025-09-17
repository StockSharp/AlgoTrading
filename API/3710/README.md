# RRS Randomness Strategy

## Overview

The **RRS Randomness Strategy** is a StockSharp port of the “RRS Randomness in Nature EA” for MetaTrader 4.  
It emulates the original expert advisor by generating random long or short market entries, applies stop-loss and take-profit levels, optionally trails profitable trades, and performs risk-based liquidation when floating losses exceed the configured threshold.

Because StockSharp uses net positions per security, simultaneous long and short exposure is not supported. The "DoubleSide" mode therefore alternates the entry direction on each opportunity instead of maintaining two hedged trades as in MetaTrader.

## Trading Logic

1. On every finished candle the strategy evaluates the latest market price obtained from trades or Level1 quotes.  
2. If there is an open position it enforces stop-loss, take-profit and trailing-stop rules and performs a portfolio risk check.  
3. When flat, it validates spread and volume constraints before opening a new trade:
   - **DoubleSide** mode alternates between long and short entries.  
   - **OneSide** mode follows the original EA rule: a random integer in `[0,5]` opens longs for values `1` or `4` and shorts for `0` or `3`.
4. Trade volumes are drawn uniformly between the configured minimum and maximum and are aligned to the instrument volume step.

## Parameters

| Group | Name | Description |
|-------|------|-------------|
| General | `Mode` | Trading mode: alternate entries (`DoubleSide`) or random gated entries (`OneSide`). |
| Lot Settings | `MinVolume` / `MaxVolume` | Volume range for randomly generated trades. |
| Protection | `TakeProfitPoints` | Take-profit distance in price steps. |
| Protection | `StopLossPoints` | Stop-loss distance in price steps. |
| Protection | `TrailingStartPoints` | Profit distance that enables trailing stop management. |
| Protection | `TrailingGapPoints` | Offset between market price and trailing stop. |
| Filters | `MaxSpreadPoints` | Maximum allowed spread (in price steps) for opening new positions. |
| Filters | `SlippagePoints` | Informational slippage setting (not enforced automatically). |
| Risk Management | `MoneyRiskMode` | Choose between fixed currency loss or percent of portfolio value. |
| Risk Management | `RiskValue` | Amount of risk (currency or percent depending on the mode). |
| General | `TradeComment` | Informational comment attached to generated orders. |
| General | `CandleType` | Candle series driving the decision loop. |

## Notes

- The strategy relies on market data subscriptions for candles, Level1 quotes and trades. Ensure the selected data type is available for the chosen security.  
- Trailing stop logic mirrors the MQL implementation: it activates after the price gains `TrailingStartPoints + TrailingGapPoints` steps and then follows price at a distance of `TrailingGapPoints`.  
- Risk management compares floating PnL with the configured loss threshold and liquidates the position when the threshold is breached.

