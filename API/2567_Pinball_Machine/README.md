# Pinball Machine Strategy

## Overview
The **Pinball Machine Strategy** is a playful conversion of the MetaTrader 5 expert advisor "Pinball machine (barabashkakvn's edition)". Instead of analyzing market structure, the strategy emulates a lottery machine: every finished candle triggers several random draws that may result in a trade if two numbers match. The StockSharp port keeps the spirit of the original expert while adapting money management and execution to the high-level API.

## Trading Logic
1. **Trigger** – the strategy works on the timeframe defined by `Candle Type`. When a candle is completed the random process runs once.
2. **Random draws** – four integers in the range 0–100 are generated. A long setup appears if the first pair matches and a short setup appears if the second pair matches. Because the draws are independent it is possible (although rare) to generate both signals on the same candle.
3. **Order eligibility** – the strategy only places a new order when no position is currently open. This keeps the net exposure single-sided, unlike the hedging behaviour of the MQL original.
4. **Stop/target distances** – for each order two additional random numbers in the range defined by `Min Offset Points` and `Max Offset Points` are produced. They determine the distance (in price steps) for the stop-loss and take-profit levels around the entry price.
5. **Position sizing** – capital at risk is limited by the `Risk Percent` parameter. The strategy estimates the portfolio value (preferring `CurrentValue`, then `CurrentBalance`, then `BeginValue`) and divides the permitted risk by the price distance to the stop. When the calculation is not possible or would result in zero size, the fallback is the strategy `Volume` (defaulting to 1 lot).
6. **Order execution** – market orders are issued via `BuyMarket` / `SellMarket`. Candle close price is used as a proxy for the entry quote because tick-level Bid/Ask data is not available in the candle-driven workflow.
7. **Trade management** – stop-loss and take-profit levels are checked on every finished candle. If price penetrates a level the position is closed by a market order, mirroring the behaviour of protective orders in the MetaTrader version.

## Parameters
- **Risk Percent** – percentage of the portfolio value that can be lost if the stop-loss is hit. Values above zero enable risk-based position sizing.
- **Min Offset Points / Max Offset Points** – inclusive bounds (expressed in price steps) for randomly selecting stop and target distances. Both parameters must stay positive; the implementation automatically swaps them if the minimum exceeds the maximum.
- **Candle Type** – the data series that drives the random engine. Any `DataType` compatible with `SubscribeCandles` can be used (minute candles by default).

## Differences from the MetaTrader Version
- **Event source** – the MT5 expert works on every tick. The StockSharp strategy evaluates the random lottery on finished candles to follow the recommended high-level API approach.
- **Hedging** – MetaTrader can accumulate multiple positions on both sides. The port limits itself to a single net position (long, short or flat) because StockSharp strategies are typically netted.
- **Money management** – the original relied on `CMoneyFixedMargin`. The C# version reproduces the idea using portfolio metrics and percent risk sizing.
- **Order placement** – explicit slippage and retry loops are unnecessary in StockSharp and were removed. Market orders are sent once the environment reports readiness (`IsFormedAndOnlineAndAllowTrading`).

## Usage Notes
- Ensure the selected security exposes a valid `PriceStep`. If none is available the strategy falls back to a step of 1 to keep the simulation running.
- Because the system is intentionally random, the performance will vary heavily between backtests. Use the strategy mainly for experimenting with infrastructure, risk handling, or Monte Carlo style randomness.
- Adjust the candle timeframe to control how frequently trades may appear. Shorter candles increase the number of lotteries per session.
- The strategy draws both candles and executed trades on a chart area when charting is available, which helps diagnose how often the random conditions are met.

## Conversion Notes
- Original file: `MQL/17744/Pinball machine.mq5`.
- Maintained all input controls (risk percent, stop and target ranges) in parameter form suitable for optimization inside StockSharp.
- Random seed uses the platform default (`Random()`), which is equivalent to the `MathSrand(GetTickCount())` call from the MetaTrader expert.
