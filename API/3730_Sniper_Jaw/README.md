# Sniper Jaw Strategy

The **Sniper Jaw Strategy** ports the MetaTrader 4 expert advisor `SniperJawEA.mq4` to StockSharp's high-level strategy API. The system analyses Bill Williams' Alligator indicator on the candle median price. A trade is only initiated when the three smoothed moving averages (jaw, teeth, and lips) are stacked in strict bullish or bearish order and all of them advance in the same direction compared with the previous finished candle.

## Trading Logic

1. **Alligator reconstruction** – three `SmoothedMovingAverage` instances calculate the jaw, teeth, and lips on the candle median `(High + Low) / 2`. Each line can be shifted forward by its own number of bars to mirror MetaTrader's plotting.
2. **Trend confirmation** – a long bias is produced when the shifted values satisfy `jaw < teeth < lips` **and** each line is higher than on the previous candle. A short bias needs `jaw > teeth > lips` with all three lines moving lower compared with the prior bar.
3. **Entry management** – the strategy opens only one position at a time. When `UseEntryToExit` is enabled and a new opposite signal fires, the current exposure is flattened first and the new order is sent on the next signal.
4. **Protective exits** – stop-loss and take-profit distances are defined in pips and converted using the security `PriceStep`. Both long and short positions are supervised on every finished candle and closed once either threshold is reached.
5. **Signal throttling** – the original EA prevented duplicate entries by checking the bar timestamp. The port stores the last signal candle time and skips additional orders during the same bar.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Trade size in lots or contracts passed to `BuyMarket`/`SellMarket`. |
| `EnableTrading` | `true` | Master switch that allows disabling new entries while keeping risk management active. |
| `UseEntryToExit` | `true` | Closes an existing position before arming an opposite signal. Mirrors the "Entry to Exit" flag of the EA. |
| `StopLossPips` | `20` | Distance of the protective stop from the entry price. Zero disables the stop. |
| `TakeProfitPips` | `50` | Distance of the profit target from the entry price. Zero disables the target. |
| `MinimumBars` | `60` | Required number of finished candles before the first signal is evaluated. |
| `JawPeriod` / `TeethPeriod` / `LipsPeriod` | `13 / 8 / 5` | Length of the smoothed moving averages forming the Alligator lines. |
| `JawShift` / `TeethShift` / `LipsShift` | `8 / 5 / 3` | Forward shift (in bars) used to align the Alligator buffers with the MetaTrader version. |
| `CandleType` | `1 hour time frame` | Primary candle series subscription. Adjust to match the chart used in MetaTrader. |

## Usage Notes

- The implementation only evaluates finished candles (`CandleStates.Finished`) to avoid partially formed values.
- Stop and target levels are tracked internally; the strategy emits market orders to flatten the position when a level is violated.
- Price step conversion follows the common Forex convention: 5- and 3-decimal symbols treat a pip as ten price steps.
- Add the strategy to a scheme together with a connector, portfolio, and security configuration. After starting the strategy, the chart panel will display the candle series and the reconstructed Alligator lines for quick visual validation.
