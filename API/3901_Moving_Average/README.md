# Moving Average Strategy

## Overview

This strategy is a StockSharp high-level port of the classic **Moving Average** expert advisor that ships with MetaTrader 4. The system observes completed candles and compares them with a shifted simple moving average (SMA) to detect direction changes. Orders are always executed at market, and the strategy stays in the market with at most one open position at any time.

## Trading Logic

1. Subscribe to candles of the configurable timeframe (default: 5 minutes) and calculate an SMA with the requested period.
2. Shift the SMA by the specified number of completed candles to emulate the original `iMA` function behaviour.
3. Evaluate the previous finished candle:
   - **Bullish cross** (open below the shifted SMA and close above) triggers a long entry when no position is open.
   - **Bearish cross** (open above and close below the shifted SMA) triggers a short entry when no position is open.
4. Manage exits using the same cross rules:
   - A long position is closed when the last candle crosses below the shifted SMA.
   - A short position is closed when the last candle crosses above the shifted SMA.
5. Only one position can exist at any time, matching the behaviour of the original EA that alternated between buy and sell orders.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle series used for calculations. Any time-frame `DataType` can be selected. | 5-minute time frame |
| `MovingPeriod` | Number of candles for the SMA length. | 12 |
| `MovingShift` | Offset of the SMA value in completed candles. Emulates the `shift` argument of `iMA`. | 6 |
| `BaseVolume` | Default order volume for entries. The same volume is used for both long and short trades. | 1 |

## Indicator Handling

- A `SimpleMovingAverage` indicator is created in `OnStarted` and bound to the candle subscription through the high-level `Bind` API.
- The raw SMA output is buffered in a small FIFO queue to obtain the value from `MovingShift` candles ago. No manual indicator recalculation is performed.
- The queue retains only `MovingShift + 1` values, so memory usage remains constant even for large shifts.

## Order and Risk Management

- Orders are placed with `BuyMarket`/`SellMarket` and are sized by the `BaseVolume` parameter. When closing, the current absolute position size is used to ensure a full exit.
- The original MetaTrader implementation dynamically adjusted lot size based on free margin and recent losses. The StockSharp port keeps the logic deterministic and delegates position sizing to the user through the `BaseVolume` parameter. This avoids relying on broker-specific account metrics while preserving the entry/exit rules.

## Conversion Notes

- Signals are evaluated on the **previous** candle, matching the `Volume[0] == 1` check from MetaTrader that waited for a new bar before reacting.
- Only completed candles (`CandleStates.Finished`) are processed to avoid premature trades.
- The strategy uses the StockSharp chart helpers to plot candles, indicator values, and trade markers when a chart area is available.

## Usage

1. Compile the strategy inside StockSharp Designer, Shell, or Runner.
2. Select the desired instrument and assign a portfolio.
3. Configure the parameters if different time frames, lengths, or volumes are required.
4. Start the strategy; it will subscribe to the chosen candle series, monitor SMA crosses, and trade accordingly.

## Further Ideas

- Add protective stops or take-profit levels using `StartProtection` if risk management beyond the basic reversal exit is required.
- Replace the simple SMA with another indicator (EMA, LWMA, etc.) by modifying the indicator instance while keeping the existing subscription workflow.
- Introduce position scaling rules by adjusting the `GetEntryVolume` method.
