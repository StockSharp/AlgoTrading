# Two Pair Correlation Strategy

## Overview

The **Two Pair Correlation Strategy** ports the MetaTrader expert advisor *"2-Pair Correlation EA"* (package `MQL/52043`) to the StockSharp high-level API. It watches the bid prices of two highly correlated crypto symbols (BTCUSD as the primary leg and ETHUSD as the hedge leg) and performs a market-neutral trade when their spread deviates from a configurable threshold.

### Core workflow

1. **Risk gating** – portfolio equity is monitored continuously. If the drawdown from the historical peak exceeds `MaxDrawdownPercent`, new trades are suspended until equity recovers above `RecoveryPercent` of the peak value.
2. **Volatility filter** – both instruments feed a 5-minute candle stream into an `AverageTrueRange` indicator of length `AtrPeriod`. Trading is skipped when either ATR exceeds `PriceDifferenceThreshold * 0.01`, mimicking the "high volatility pause" from the MQL code.
3. **Spread detection** – the strategy subscribes to level-one data for both instruments and evaluates the bid-price spread on every update. When `Bid(BTCUSD) - Bid(ETHUSD) > PriceDifferenceThreshold`, it buys BTCUSD and sells ETHUSD. When the spread drops below `-PriceDifferenceThreshold`, the positions are reversed (short BTCUSD, long ETHUSD).
4. **Dynamic lot sizing** – the per-leg volume is derived from `RiskPercent` of the current portfolio equity, divided by the synthetic stop distance `StopLossPips * PriceStep`. The result is normalised with the exchange volume constraints before orders are sent.
5. **Basket exit** – the total floating profit of both legs is tracked in account currency. Once it reaches `MinimumTotalProfit`, the strategy closes the entire pair regardless of the entry direction.

## Required market data

- **Level1** (best bid/ask) for both the primary security (`Security`) and the hedge security (`SecondSecurity`).
- **Candles** of type `AtrCandleType` (defaults to 5-minute time-frame) for the same two instruments to feed the ATR filter.

Ensure the securities expose meaningful `PriceStep`, `StepPrice`, `VolumeStep`, and min/max volume values so that the lot sizing and profit conversion mirror the MetaTrader behaviour.

## Parameters

| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `SecondSecurity` | `Security` | — | Hedge instrument (ETHUSD in the original EA). |
| `MaxDrawdownPercent` | `decimal` | `20` | Drawdown threshold that pauses new trades. |
| `RiskPercent` | `decimal` | `2` | Portfolio share risked per trade for position sizing. |
| `PriceDifferenceThreshold` | `decimal` | `100` | Bid-price divergence required to open the pair. |
| `MinimumTotalProfit` | `decimal` | `0.30` | Profit target in account currency for closing both legs. |
| `AtrPeriod` | `int` | `14` | ATR length for the volatility filter. |
| `RecoveryPercent` | `decimal` | `95` | Percentage of the peak equity required to resume trading after a drawdown. |
| `StopLossPips` | `int` | `50` | Synthetic stop used to translate `RiskPercent` into lots. |
| `AtrCandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Candle series used for ATR calculation. |

## Files

- `CS/TwoPairCorrelationStrategy.cs` – strategy implementation built on the high-level API.
- `README.md` – this documentation (English).
- `README_cn.md` – documentation in Chinese.
- `README_ru.md` – documentation in Russian.
