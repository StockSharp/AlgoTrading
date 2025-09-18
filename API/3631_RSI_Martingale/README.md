# RSI Martingale

## Overview
RSI Martingale is a port of the MetaTrader 5 expert advisor `RSI&Martingale1.5`. The strategy searches for momentum reversals by waiting until the Relative Strength Index (RSI) reaches an extreme value within a configurable lookback window. When an extreme appears, it opens a trade in the direction of the expected mean reversion and exits when RSI crosses the 50 midline or when a fixed stop/take target is reached. A martingale module can optionally reopen the position in the opposite direction with an increased volume after a losing trade. Daily profit and loss limits, along with hourly filters, make it possible to suspend trading during riskier sessions or after meeting capital preservation goals.

## Strategy logic
### RSI extremes
* **Indicator** – a single RSI calculated on the selected candle type. The indicator must be formed (enough historical data) before trades are considered.
* **Minimum detection** – if the latest RSI value is less than or equal to every RSI value inside the configured `Bars For Extremes` window and the value is below 50, the strategy opens a long position.
* **Maximum detection** – if the latest RSI value is greater than or equal to every value inside the lookback window and the value is above 50, the strategy opens a short position.

### Position management
* **Exit trigger** – positions are closed when RSI crosses the neutral 50 line to the opposite side (longs exit above 50, shorts exit below 50).
* **Fixed targets** – optional stop-loss and take-profit distances expressed in pips. When enabled, the strategy compares the most recent candle’s high/low to those target prices and closes the position if either level is hit.
* **Volume alignment** – every order volume is aligned to the security’s step, minimum, and maximum settings before submission.

### Martingale recovery
* **Trigger** – after a position closes with a negative profit, the strategy remembers the direction and volume of the losing trade.
* **Re-entry** – on the next eligible candle, and only if no position is open, it can immediately open a trade in the opposite direction. The volume is either the losing volume multiplied by the `Martingale Multiplier` or the base `Initial Volume` depending on the `Enable Martingale` switch.
* **Reset** – once the martingale order is submitted, the stored loss information is cleared to avoid repeated attempts.

### Daily capital control
* **Baseline** – the strategy captures the account equity at the start of each trading day and resets the suspension flag.
* **Monitoring window** – daily limits are evaluated only between `Daily Control Start` and `Daily Control End` hours.
* **Suspension** – if the equity grows beyond `Daily Profit %` or drops below `Daily Loss %`, the strategy closes any open position and skips new trades until the next day.

### Session filters
* **Trading window** – new positions are allowed only when the current hour is between `Trading Start` and `Trading End` (inclusive).
* **Hour avoidance** – 24 boolean parameters mirror the source EA’s “news avoidance” settings and block trading during the selected hours.

## Parameters
* **Initial Volume** – base order volume for standard entries.
* **RSI Period** – number of periods used by the RSI indicator.
* **Bars For Extremes** – how many finished candles are scanned when looking for the latest RSI minimum or maximum.
* **Take Profit (pips)** – distance to the fixed take-profit; set to `0` to disable.
* **Stop Loss (pips)** – distance to the fixed stop-loss; set to `0` to disable.
* **Enable Martingale** – enables the martingale recovery module after a losing trade.
* **Martingale Multiplier** – multiplier applied to the previous losing volume when martingale is active.
* **Daily Targets** – toggles the daily profit/loss suspension logic.
* **Daily Profit %** – profit percentage that halts trading for the current day.
* **Daily Loss %** – loss percentage that halts trading for the current day.
* **Daily Control Start / Daily Control End** – hour boundaries for evaluating the daily limits.
* **Trading Start / Trading End** – hour boundaries that allow new positions.
* **Avoid Hour 00 … Avoid Hour 23** – disable trading during the corresponding clock hour.
* **Candle Type** – candle subscription used for the RSI indicator and all calculations.

## Additional notes
* The strategy operates on finished candles only and does not evaluate intrabar ticks.
* Daily profit calculations combine realized strategy PnL with floating PnL based on the latest close price.
* There is no Python implementation for this strategy in the package; only the C# version is provided.
