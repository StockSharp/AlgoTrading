# Tops Bottoms Trend RSI Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor "Tops bottoms trend and rsi ea". It monitors finished candles of the selected timeframe, searches for emerging trend tops or bottoms within a configurable lookback window, and confirms each opportunity with a Relative Strength Index (RSI) filter. When the criteria are met the strategy opens a single market order and immediately assigns protective stop-loss and take-profit levels derived from pip-based distances.

## Trading Logic
- **Data source** – the algorithm subscribes to the configured candle type and evaluates only finished candles to avoid using incomplete data.
- **Bottom detection (long setup)** – the close of the latest candle must be at least `BuyTrendPips` pips below the high of the candle `BuyTrendCandles` bars ago. All intermediate lows must stay above the current close, and the quality filter (`BuyTrendQuality`) requires that recent highs do not deviate too much from the reference high. When this structure forms and the previous candle's RSI value is below `BuyRsiThreshold`, the strategy opens a long position with volume `BuyVolume`.
- **Top detection (short setup)** – the close of the latest candle must be at least `SellTrendPips` pips above the low of the candle `SellTrendCandles` bars ago. Intermediate highs must remain below the current close while the quality filter (`SellTrendQuality`) keeps recent lows close to the reference low. If the previous candle's RSI value exceeds `SellRsiThreshold`, the strategy opens a short position with volume `SellVolume`.
- **Risk management** – after each entry the strategy stores the fill price and calculates pip-based protective levels. Stop-loss offsets use `BuyStopLossPips` or `SellStopLossPips`. Take-profit distances are primarily derived from the stop via `BuyTakeProfitPercentOfStop` or `SellTakeProfitPercentOfStop`. If the long take-profit percentage is disabled (`0`) the fixed `BuyTakeProfitPips` distance is used instead. Whenever subsequent candles touch the corresponding stop or take-profit levels the position is closed with a market order.
- **Position control** – the system keeps at most one open position. New signals are ignored while a position or active order exists. RSI confirmation always relies on the previous candle (one-bar shift), mirroring the original EA.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `BuyVolume` | Order volume used for long positions. | `0.01` |
| `BuyStopLossPips` | Stop-loss distance for long trades in pips. | `20` |
| `BuyTakeProfitPips` | Fixed take-profit distance in pips for longs when percentage mode is disabled. | `5` |
| `BuyTakeProfitPercentOfStop` | Take-profit as a percentage of the long stop-loss distance. | `100` |
| `SellVolume` | Order volume used for short positions. | `0.01` |
| `SellStopLossPips` | Stop-loss distance for short trades in pips. | `20` |
| `SellTakeProfitPercentOfStop` | Take-profit as a percentage of the short stop-loss distance. | `100` |
| `SellTrendCandles` | Number of candles inspected when searching for new tops. | `10` |
| `SellTrendPips` | Minimum advance above the reference low required for a short setup (pips). | `20` |
| `SellTrendQuality` | Trend-quality filter for short setups (clamped to the 1–9 range). | `5` |
| `BuyTrendCandles` | Number of candles inspected when searching for new bottoms. | `10` |
| `BuyTrendPips` | Minimum decline below the reference high required for a long setup (pips). | `20` |
| `BuyTrendQuality` | Trend-quality filter for long setups (clamped to the 1–9 range). | `5` |
| `BuyRsiPeriod` | RSI period used for long confirmations. | `14` |
| `BuyRsiThreshold` | RSI oversold threshold that must be crossed from above to enable long entries. | `40` |
| `SellRsiPeriod` | RSI period used for short confirmations. | `14` |
| `SellRsiThreshold` | RSI overbought threshold that must be crossed from below to enable short entries. | `60` |
| `CandleType` | Timeframe of the candles processed by the strategy. | `30-minute time frame` |

## Notes
- Pip distances are converted to prices using the security's `PriceStep`. Five-digit and fractional-pip forex quotes are normalised to the classic pip size, replicating the conversion rules from the original EA.
- Because the RSI confirmation uses the previous candle (shift = 1), the strategy needs at least one fully formed RSI value before it can trade. The first few candles after startup are therefore ignored.
- The logic cancels all protective levels whenever a position is fully closed, ensuring that the next entry starts with fresh risk parameters.
