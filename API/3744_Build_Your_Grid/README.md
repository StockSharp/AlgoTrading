# Build Your Grid Strategy

This folder contains the StockSharp high level API conversion of the MetaTrader expert advisor **BuildYourGridEA v1.8**. The strategy recreates the original grid trading behaviour by stacking market orders in both directions, dynamically adjusting the spacing, lot progression and equity protection rules.

## Core idea

1. **Initial entries** – the strategy opens the first buy and/or sell orders according to the `OrderPlacement` parameter.
2. **Grid expansion** – additional orders are triggered when price moves by the configured step. The distance can remain constant, grow geometrically or double with every entry.
3. **Money management** – volumes can follow the first lot, grow geometrically or double every time. Optional balance based auto lot sizing reproduces the MetaTrader risk factor formula.
4. **Risk controls** – the grid can be closed either by reaching a target in pips or account currency, by hitting a drawdown threshold or by placing hedge-balancing trades when equity drawdown exceeds a percentage of the current balance.
5. **Spread filter** – a level1 subscription keeps track of the best bid/ask spread; new orders are suppressed when the spread exceeds `MaxSpread` pips.

## Implementation notes

- High level API only: the strategy relies on `SubscribeCandles`, `SubscribeLevel1`, `BuyMarket` and `SellMarket` to manage trades.
- Trade state is stored in two lightweight lists that mimic MetaTrader's ticket information. They hold the open price and volume of each leg and are refreshed after every fill (`OnNewMyTrade`).
- Hedge mode is emulated by reducing the net exposure. When `PlaceHedgeOrder` becomes active, the strategy sends balancing trades with the volume difference multiplied by `MuliplierHedgeLot`. This matches the EA behaviour on netting accounts.
- Pip, step and currency calculations reuse the security's `PriceStep` and `StepPrice`. When these values are not available, safe defaults are applied so that backtests still run.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Primary timeframe used to evaluate the grid. |
| `OrderPlacement` | Which directions (buy/sell) are allowed. |
| `NextOrder` | Whether new orders follow the trend or fade it. |
| `PipsForNextOrder` | Base grid spacing in pips. |
| `StepMode` | Progression applied to the grid distance. |
| `ProfitTarget` | Metric (pips or currency) used to close the grid in profit. |
| `PipsCloseInProfit` | Target in pips when `ProfitTarget = TargetInPips`. |
| `CurrencyCloseInProfit` | Target in account currency when `ProfitTarget = TargetInCurrency`. |
| `LossMode` | How to react when the floating loss exceeds `PipsForCloseInLoss`. |
| `PipsForCloseInLoss` | Drawdown threshold in pips. |
| `PlaceHedgeOrder` | Enable hedge balancing. |
| `LevelLossForHedge` | Drawdown percentage that triggers hedge balancing. |
| `MuliplierHedgeLot` | Volume multiplier for hedge trades. |
| `AutoLotSize` | Enable automatic lot calculation from balance and `RiskFactor`. |
| `RiskFactor` | Risk percentage per 100,000 units of balance. |
| `ManualLotSize` | First lot when auto lot is disabled. |
| `LotProgression` | Lot progression (static, geometrical, exponential). |
| `MaxMultiplierLot` | Maximum multiplier applied to the first lot. |
| `MaxOrders` | Maximum simultaneous open positions (0 = unlimited). |
| `MaxSpread` | Maximum allowed spread in pips before new orders are blocked. |

## Differences from the original EA

- StockSharp uses netting accounts, therefore hedge trades close part of the opposite exposure instead of holding independent buy/sell tickets. The balancing logic matches the effective result of the MetaTrader implementation.
- Visual chart objects and sound alerts from the MQL code are intentionally omitted.
- Slippage and manual spread validation use the best bid/ask values supplied by the level1 subscription.

## Usage

1. Attach the strategy to a portfolio and a security that supports both candles and level1 quotes.
2. Adjust the parameters to match the desired grid behaviour.
3. Start the strategy – it will automatically track fills and manage the grid according to the configured rules.

