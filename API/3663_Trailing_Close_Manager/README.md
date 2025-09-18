# Trailing Close Manager Strategy

## Overview

The **TrailingCloseManagerStrategy** replicates the behaviour of the MetaTrader expert advisor *Trailing Only with Close All Button v2*. It does not search for entries. Instead it supervises open positions, applies pip-based initial stops, trails the exit levels, and exposes manual "buttons" through boolean parameters so that an operator can instantly close all, only profitable, or only losing trades.

## Key Features

- **Initial protective distances** – when a position appears the strategy assigns the configured stop-loss and take-profit offsets expressed in pips. The offsets are recalculated every time the position size changes.
- **Dynamic trailing stop** – after the market advances by the trailing distance, the stop is activated and follows price respecting the trailing step. Both long and short positions are supported.
- **Manual close buttons** – three boolean parameters emulate the chart buttons from the original EA: `Close All`, `Close Profit`, and `Close Losing`. When toggled they schedule a configurable number of close attempts separated by a delay.
- **Profit/loss guard** – global thresholds close the entire portfolio once the floating profit grows above the configured positive level or the floating loss drops below the negative limit.
- **Retry management** – each close routine can retry several times with a delay (default five attempts and 500 ms) to mimic the robust loop found in the MQL implementation.

## Parameters

| Name | Description |
| --- | --- |
| `Stop Loss (pips)` | Initial stop-loss distance in adjusted pips. Set to zero to disable.
| `Take Profit (pips)` | Initial take-profit distance in adjusted pips. Set to zero to disable.
| `Trailing Stop (pips)` | Distance between current price and the trailing stop once activated.
| `Trailing Step (pips)` | Additional movement required before the trailing stop moves again.
| `Close Profit Threshold` | Close every position when floating profit reaches this value.
| `Close Loss Threshold` | Close every position when floating loss reaches (is lower than) this negative value.
| `Retry Count` | How many times the strategy repeats a manual close request.
| `Retry Delay` | Delay between retries of the manual close routine.
| `Close All` | Manual button that flattens the entire portfolio.
| `Close Profit` | Manual button that closes only positions with positive floating PnL.
| `Close Losing` | Manual button that closes only positions with negative floating PnL.

## Usage Tips

1. **Attach to an instrument with existing trades.** The strategy expects positions to be opened by the operator or other automated systems.
2. **Adjust pip distances to the instrument.** The pip size adapts to three- and five-digit quotes by multiplying the price step by ten, matching the original EA behaviour.
3. **Monitor the logs.** Informational messages are written whenever trailing is activated or adjusted, when manual buttons trigger, and when retry attempts are exhausted.
4. **Combine with other strategies.** Because the strategy focuses on position management, it can be run alongside entry algorithms to protect their trades automatically.

## Conversion Notes

- The graphical chart buttons from MetaTrader were converted into boolean parameters that reset to `false` after activation.
- The trailing logic uses last-trade prices delivered by a trade subscription, aligning with the tick-based execution of the original script.
- Stop-loss, take-profit, and trailing levels are applied virtually; the strategy exits via market orders instead of modifying protective orders because this is the recommended high-level API approach.
