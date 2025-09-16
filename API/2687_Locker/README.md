# Locker
[Русский](README_ru.md) | [中文](README_cn.md)

Grid-based hedging strategy that alternates long and short market orders to lock floating losses and capture a small percentage profit on the account balance.

## Trading logic
* Opens the first long position with the configured starting volume as soon as the first candle closes.
* Tracks every subsequent entry and keeps an internal ledger of buy and sell legs to estimate combined unrealized and realized profit.
* If the number of active legs reaches eight, the strategy closes the earliest available buy/sell pair to keep exposure under control before doing anything else on that candle.
* When the combined profit rises above the target percentage of the portfolio value, it exits all remaining positions and resets the internal state.
* When the combined profit drops below the negative target, it measures the distance between the latest entry price and the current market price. If price has moved upward by the configured step it adds a new short leg; if price has moved downward by the same distance it adds a new long leg.
* Every close uses market orders in the opposite direction of the recorded entry so the hedge is neutralized immediately.

## Parameters
* **Profit %** – percentage of the current portfolio value that should be locked in before flattening the book.
* **Start Volume** – quantity used for the very first long entry that seeds the grid.
* **Step Volume** – quantity submitted for every hedging order once the loss threshold is breached.
* **Step Points** – number of price steps between grid levels; multiplied by the instrument's price step to calculate the actual price distance.
* **Enable Automation** – master switch that pauses all trading logic when disabled.
* **Candle Type** – candle series used to trigger the decision logic on every finished bar.

The conversion replicates the original MetaTrader expert logic while adapting order placement to the StockSharp high-level API and storing detailed trade state inside the strategy so that profit calculation matches the MQL version.
