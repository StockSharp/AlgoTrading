# Perceptron AC Strategy

This strategy implements a simple perceptron on top of the Accelerator Oscillator (AC).
The AC value of the current candle and of three past offsets are multiplied by adjustable weights.
The sum of these products forms the perceptron output that determines the trade direction.

## How It Works

1. Calculate the Accelerator Oscillator (AC) from the difference between the Awesome Oscillator and its 5-period SMA.
2. Store the latest 22 AC values to access offsets of 0, 7, 14 and 21 bars.
3. Compute the perceptron output:
   `P = (X1-100)*AC[0] + (X2-100)*AC[7] + (X3-100)*AC[14] + (X4-100)*AC[21]`.
4. If `P > 0` open or maintain a long position; if `P < 0` open or maintain a short position.
5. When a position gains at least `StopLoss` points beyond the initial stop level:
   - If the perceptron flips direction, reverse the position.
   - Otherwise trail the stop to the new price minus/plus `StopLoss`.

## Parameters

- **X1** – weight for the current AC value (default 288).
- **X2** – weight for AC 7 bars ago (default 216).
- **X3** – weight for AC 14 bars ago (default 144).
- **X4** – weight for AC 21 bars ago (default 72).
- **Stop Loss** – trailing and reversal threshold in price units (default 300).
- **Volume** – order volume (default 1).
- **Candle Type** – candle series to subscribe to (default 5-minute).

## Trading Rules

- Enter long when `P > 0` and no position is open.
- Enter short when `P < 0` and no position is open.
- For open positions, move the stop loss after the price moves in profit by `Stop Loss * 2`.
- Reverse the position if the perceptron output changes sign at that time.

## Original Version

Converted from the MQL4 script `auto_m5.mq4` located in `MQL/11102`.
