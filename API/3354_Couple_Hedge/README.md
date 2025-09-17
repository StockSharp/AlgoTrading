# Couple Hedge Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Couple Hedge strategy is a multi-pair grid system converted from the **MT4 CoupleHedgeEA v2.5** expert advisor. It accepts a ranked list of currencies, builds all possible FX pairs between them and operates hedged baskets on the detected combinations. The logic keeps the baskets market-neutral by opening both bullish and bearish legs depending on the relative strength of each pair. Losses are handled with adaptive scaling rules that mirror the original expert's "step" and "progression" parameters.

While the MT4 version relies on a proprietary graphical interface and a large amount of on-chart statistics, this StockSharp port focuses on the execution logic. Every important money-management option—operation mode, step sizing, profit/loss handling and lot progression—is exposed through parameters so the behaviour remains close to the source code.

## Original idea
- Trade several currency pairs simultaneously to diversify risk across combinations of three to five currencies.
- Maintain both plus (long-bias) and minus (short-bias) baskets and rebalance them when the relative strength ranking changes.
- Scale into losing baskets using configurable step distances and lot multipliers until the global basket profit turns positive.
- Close the baskets when a dollar-per-lot profit target is reached, optionally delaying the exit by a few candles to filter out noise.

## StockSharp implementation
- Resolves all FX pairs that can be created from the `CurrencyTrade` string (e.g. `EUR/GBP/USD` generates EURGBP, EURUSD and GBPUSD). If a symbol cannot be found it is skipped and the main security is used as a fallback.
- Uses a moving average (period defined by `TrendPeriod`) and an ATR (period defined by `AtrPeriod`) to evaluate relative strength. The ATR-normalized deviation must exceed `SignalThreshold` before a new basket is opened.
- Supports the original side filter (`TradeOnlyPlus`, `TradeOnlyMinus`, `TradePlusAndMinus`). Plus baskets buy the pair, minus baskets sell it.
- Adds new layers when the floating loss per lot exceeds the configured step. Manual steps use `StepOpenNextOrders`; automatic steps multiply the ATR value by the same parameter. Step and lot progression modes (`Statical`, `Geometrical`, `Exponential`) are available.
- Basket and ticket-based profit targets are implemented. The delay counters emulate the EA's tick wait (`DelayCloseProfit`, `DelayCloseLoss`).
- Loss handling supports closing the whole ticket or shaving off the first layer while keeping the remainder open.
- Automatic lot sizing approximates the original "risk factor" option by allocating a percentage of the portfolio equity per basket. When disabled, `ManualLotSize` is used.

## Trading rules
- **Entry**
  - Compute deviation = (Close - SMA) / ATR.
  - Long basket (plus side) if deviation ≥ `SignalThreshold` and the side filter allows it.
  - Short basket (minus side) if deviation ≤ -`SignalThreshold` and the side filter allows it.
- **Scaling**
  - Enabled when `OpenOrdersInLoss` is not `NotOpenInLoss`.
  - Triggered once the unrealised loss per lot reaches the current step threshold.
  - Additional volume follows the selected lot progression.
  - Respect `MaximumOrders` (0 means unlimited).
- **Profit exit**
  - Ticket mode: close the specific pair once its profit per lot exceeds `TargetCloseProfit` for `DelayCloseProfit` candles.
  - Basket mode: close all watched pairs once the combined profit reaches the target for the configured delay.
  - Hybrid mode: both ticket and basket checks must agree.
  - If `TypeOperation` is `CloseInProfitAndStop`, the strategy stops after a profitable exit.
- **Loss exit**
  - Activated when loss per lot ≥ `TargetCloseLoss` for `DelayCloseLoss` candles.
  - `WholeTicket` closes the entire basket; `OnlyFirstOrder` removes a single base lot; `NotCloseInLoss` ignores the signal.

## Parameters (defaults)
- `TypeOperation` = `NormalOperation`
- `OpenOrdersInLoss` = `OpenWithAutoStep`
- `StepOpenNextOrders` = 50 (dollars per lot)
- `StepOrdersProgress` = `GeometricalStep`
- `LotOrdersProgress` = `StaticalLot`
- `TypeCloseInProfit` = `BasketOrders`
- `TargetCloseProfit` = 50 (dollars per lot)
- `DelayCloseProfit` = 3 candles
- `TypeCloseInLoss` = `NotCloseInLoss`
- `TargetCloseLoss` = 1000 (dollars per lot)
- `DelayCloseLoss` = 3 candles
- `MaximumOrders` = 0 (unlimited)
- `SideToOpenOrders` = `TradePlusAndMinus`
- `CurrencyTrade` = `EUR/GBP/USD`
- `AutoLotSize` = false
- `RiskFactor` = 0.1 (10% of equity when auto sizing)
- `ManualLotSize` = 0.01
- `TrendPeriod` = 34
- `AtrPeriod` = 14
- `SignalThreshold` = 0.3
- `CandleType` = 15-minute timeframe

## Limitations & notes
- Groups, manual skip lists, UI colour settings, session management and file logging from the original EA are not ported.
- The MT4 timer-based refresh is replaced with candle subscriptions; no dedicated millisecond timer is used.
- Automatic lot sizing uses a simplified equity percentage model because MT4 account leverage information is not available in StockSharp.
- The strategy assumes that the exchange connector publishes per-position PnL values; when unavailable, basket close logic may not trigger.
- Python implementation is intentionally omitted as requested.
