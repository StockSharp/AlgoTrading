# AdaptiveTrader Pro Strategy

## Overview
AdaptiveTrader Pro is a multi-timeframe trend-following strategy converted from the MetaTrader 5 expert advisor *AdaptiveTrader_Pro_Final_EA.mq5*. It combines RSI, ATR and moving averages to trade in the direction of the dominant trend while applying money-management controls.

The strategy works on a configurable primary timeframe (default 5 minutes) and confirms trend direction using a higher timeframe moving average (default 1 hour). Entries rely on oversold/overbought RSI signals that agree with both moving averages.

## Trading Rules
- **Long Entry**: When RSI falls below 30 and the candle close is above the main timeframe SMA and the higher timeframe SMA.
- **Short Entry**: When RSI rises above 70 and the candle close is below both SMAs.
- **Single Position**: Only one directional position is kept at a time. Opposite positions are closed before reversing.

## Risk and Trade Management
- **Position Sizing**: Position size is calculated from portfolio equity, risk percentage and ATR-based stop distance.
- **Stop Handling**: An ATR-based trailing stop follows price and is tightened to break-even after the trade moves in favor by a configurable ATR multiple.
- **Partial Profit**: A configurable fraction of the position is closed at a first target (ATR multiple). Remaining volume is managed by the trailing stop.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `MaxRiskPercent` | Risk percentage applied to the account per trade. | `0.2` |
| `RsiPeriod` | RSI length on the main timeframe. | `14` |
| `AtrPeriod` | ATR length on the main timeframe. | `14` |
| `AtrMultiplier` | ATR multiplier for the initial stop distance. | `1.5` |
| `TrailingStopMultiplier` | ATR multiplier used while trailing the stop. | `1.0` |
| `TrailingTakeProfitMultiplier` | ATR multiplier for the partial take-profit target. | `2.0` |
| `TrendPeriod` | SMA length on the main timeframe. | `20` |
| `HigherTrendPeriod` | SMA length on the higher timeframe. | `50` |
| `BreakEvenMultiplier` | ATR multiplier that triggers moving the stop to break-even. | `1.5` |
| `PartialCloseFraction` | Fraction of the initial position closed at the first target. | `0.5` |
| `MaxSpreadPoints` | Maximum allowed spread in price steps before opening trades. | `20` |
| `CandleType` | Primary candle type (timeframe) used for analysis. | `5 minute candles` |
| `HigherCandleType` | Higher timeframe candle type used for confirmation. | `1 hour candles` |

## Notes
- The strategy uses StockSharp high-level API with candle subscriptions and indicator binding.
- Spreads are monitored through the best bid/ask quotes; trading is suspended until the spread is within the configured limit.
- Python implementation is intentionally omitted per instructions.
