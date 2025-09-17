# Divergence Trader (Classic Conversion)

This strategy reproduces the behaviour of the MetaTrader 4 expert advisor **Divergence Trader** inside the StockSharp high level API. Two simple moving averages are calculated on the selected candle price (open by default). The system monitors how the distance between the fast and slow averages changes from one bar to the next:

* When the spread widens to the upside and the divergence value stays between the *Buy Threshold* and the *Stay Out Threshold*, a long position is opened or an existing short position is covered.
* When the spread widens to the downside within the mirrored thresholds, a short position is entered or an existing long trade is closed.

Only completed candles are used, matching the bar-by-bar processing of the original expert advisor. All management rules are implemented with event driven high-level calls (`BuyMarket` / `SellMarket`).

## Trading rules

1. Subscribe to the configured candle type and calculate two SMAs with periods *Fast SMA* and *Slow SMA*.
2. Compute the current spread (`fast - slow`) and compare it with the previous spread to obtain the divergence value.
3. Enter long if the divergence is positive, greater than or equal to *Buy Threshold* and less than or equal to *Stay Out Threshold*.
4. Enter short if the divergence is negative, less than or equal to `-Buy Threshold` and greater than or equal to `-Stay Out Threshold`.
5. Reverse an existing position whenever an opposite signal appears.
6. Restrict new entries to the local time window between *Start Hour* and *Stop Hour* (wrapping over midnight is supported).

## Risk management

* Optional fixed *Take Profit (pips)* and *Stop Loss (pips)* levels are monitored on candle highs/lows.
* The *Break-Even Trigger (pips)* moves the stop to `entry ± Break-Even Buffer` once the position gains the specified number of pips.
* The *Trailing Stop (pips)* follows the most favourable price once the trade is in profit. Setting 9999 disables the trailing stop, mirroring the original EA default.
* Basket management closes all open exposure when unrealised P&L reaches *Basket Profit* or drops below `-Basket Loss` in account currency.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Order Volume` | Volume used when a new position is opened. |
| `Fast SMA` / `Slow SMA` | Periods for the two simple moving averages. |
| `Applied Price` | Candle component forwarded into both moving averages. |
| `Buy Threshold` | Lower divergence boundary that enables long trades. |
| `Stay Out Threshold` | Upper divergence boundary above which no new trades are taken. |
| `Take Profit (pips)` / `Stop Loss (pips)` | Optional hard exits measured in pips. |
| `Trailing Stop (pips)` | Trailing distance applied after the trade becomes profitable. |
| `Break-Even Trigger (pips)` | Profit in pips required before moving the stop to break-even. |
| `Break-Even Buffer (pips)` | Additional buffer added to the break-even stop. |
| `Basket Profit` / `Basket Loss` | Global equity limits in account currency. |
| `Start Hour` / `Stop Hour` | Local trading session window. |
| `Candle Type` | Timeframe used for candle subscription and calculations. |

## Usage notes

* Attach the strategy to a security and set the candle type that matches the original chart timeframe.
* Ensure the `PriceStep`/`StepPrice` properties of the instrument are configured so that pip based controls work correctly.
* To disable features such as the trailing stop or break-even shift, keep their parameters at the legacy sentinel value (9999) or zero.
