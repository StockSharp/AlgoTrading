# Two EMA Intraday Filter Strategy

## Overview
This strategy reproduces the MetaTrader Expert Advisor **Expert_2EMA_ITF** using the StockSharp high-level API. It trades on the crossover of two exponential moving averages and uses the average true range (ATR) to set pending limit orders, protective stops and targets. An additional intraday time filter blocks entries during undesired minutes, hours or days of the week.

## Logic Summary
- Calculate fast and slow EMA values on the selected candle series.
- Detect a bullish crossover when the fast EMA rises above the slow EMA and a bearish crossover when it falls below.
- On a bullish crossover place a buy limit order offset from the slow EMA by `LimitMultiplier * ATR` plus the current spread. On a bearish crossover place a sell limit order offset in the opposite direction.
- Store stop-loss and take-profit prices using ATR multipliers so they can be submitted immediately once the entry order is filled.
- Cancel pending orders automatically if they remain unfilled for more than `ExpirationBars` candles.
- Skip signals that do not pass the intraday filter (allowed minute, hour and day checks). Bit masks can disable multiple minutes, hours or days simultaneously.

## Indicators
- **Fast EMA** – controls the sensitivity of crossover detection.
- **Slow EMA** – defines the trend direction.
- **Average True Range (ATR)** – measures market volatility and scales entry/exit price offsets.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Time frame used for calculations. | 30-minute candles |
| `FastEmaPeriod` | Period of the fast EMA. | 5 |
| `SlowEmaPeriod` | Period of the slow EMA (must be greater than the fast period). | 30 |
| `AtrPeriod` | ATR calculation period. | 7 |
| `LimitMultiplier` | ATR multiplier that shifts limit entry prices. | 1.2 |
| `StopLossMultiplier` | ATR multiplier for stop-loss placement. | 5 |
| `TakeProfitMultiplier` | ATR multiplier for take-profit placement. | 8 |
| `ExpirationBars` | Number of bars after which unfilled orders are cancelled. | 4 |
| `GoodMinuteOfHour` | Specific minute allowed for entries (-1 disables). | -1 |
| `BadMinutesMask` | Bit mask blocking minutes (bit *n* blocks minute *n*). | 0 |
| `GoodHourOfDay` | Specific hour allowed for entries (-1 disables). | -1 |
| `BadHoursMask` | Bit mask blocking hours (bit *n* blocks hour *n*). | 0 |
| `GoodDayOfWeek` | Specific day allowed for entries (-1 disables, 0 = Sunday). | -1 |
| `BadDaysMask` | Bit mask blocking days (bit *n* blocks day *n*, 0 = Sunday). | 0 |

## Order Management
1. **Entry orders** – Limit orders are registered with a price shifted from the slow EMA by the ATR-based offset. The buy order also adds the current spread if bid/ask quotes are available.
2. **Expiration** – Each pending order stores the candle index when it was created. If `ExpirationBars` is positive and the order survives beyond that many bars, it is cancelled automatically.
3. **Protective orders** – When an entry order fills the strategy cancels any previous stop/target orders, then immediately places a stop-loss and take-profit calculated from the ATR snapshot that generated the signal. Opposite protective orders are cancelled when the position is flat.

## Intraday Filter Details
- **Single allow values** – `GoodMinuteOfHour`, `GoodHourOfDay`, and `GoodDayOfWeek` restrict trading to a specific minute, hour or weekday when they are non-negative.
- **Bit masks** – `BadMinutesMask`, `BadHoursMask`, and `BadDaysMask` contain bits that disable multiple time slots at once. For example, setting `BadMinutesMask = (1 << 0) | (1 << 30)` blocks trading during minute 0 and minute 30 of each hour.
- **Combined logic** – An entry is only permitted when the current candle time passes all allow conditions and none of the masks block it.

## Differences from the Original Expert Advisor
- The StockSharp version uses pending limit orders combined with explicit stop-loss and take-profit registrations once the entry is executed, mirroring the MQL signal calculations.
- Spread compensation for buy orders uses the current `Security.BestBid/BestAsk` quotes when they are available, otherwise the offset is zero.
- Time filtering is expressed through bit masks and direct comparisons instead of MetaTrader specific time-filter helper classes.
- All trading actions leverage StockSharp high-level helpers (`BuyLimit`, `SellLimit`, `SellStop`, `BuyStop`) and automatic cancellation logic instead of manual order arrays.

## Usage Notes
- Ensure the strategy volume is set before starting the strategy; otherwise, a warning is produced and no orders are sent.
- For optimization scenarios the parameter metadata already enables tuning of EMA periods, ATR period, multipliers and expiration length.
- The strategy assumes that candle close times represent the end of the bar and uses them when evaluating intraday filters.
