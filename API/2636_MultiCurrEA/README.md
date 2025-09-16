# MultiCurrEA Strategy

## Overview

MultiCurrEA is a multi-currency breakout strategy converted from the original MetaTrader5 expert advisor. The system watches up to
three FX pairs simultaneously and trades when price interacts with Bollinger Bands calculated on high and low prices. A deal
counter controls position scaling by reallocating a configurable percentage of account equity on each new entry.

## Trading Logic

- **Indicators**
  - Two Bollinger Bands are calculated for each symbol: one on candle highs and one on candle lows.
  - Indicator values can be shifted by the `Bollinger Shift` parameter, matching the original EA behaviour.
- **Entry Rules**
  - A long position is opened when the current ask price falls below the lower band computed from lows.
  - A short position is opened when the current bid price rises above the upper band computed from highs.
  - Only one trade per bar is allowed thanks to a bar time lock.
- **Exit Rules**
  - Long positions are closed when the bid price touches the lower band derived from highs.
  - Short positions are closed when the ask price touches the upper band derived from lows.
  - If both conditions are true simultaneously the strategy stands aside and the deal counter resets.
- **Volume Scaling**
  - The volume of the first deal uses `Deal % of Equity` percent of portfolio equity divided by the current price.
  - Every additional deal on the same symbol multiplies the base volume by `Lot Increase * (dealNumber - 1)`.

Bid/ask prices are taken from level1 data. If the broker does not provide quotes the latest candle close price is used as a
fallback.

## Parameters

### Shared Risk Settings
- `Deal % of Equity` – percentage of equity allocated to the first trade.
- `Lot Increase` – scaling factor for the second and subsequent trades.

### Per-Symbol Settings
Each of the three slots exposes the following inputs:
- `Security` – instrument to trade.
- `Enabled` – turn trading on/off for the slot.
- `Timeframe` – candle type for indicator calculations.
- `Bollinger Period` – number of candles in the moving average.
- `Bollinger Shift` – number of completed bars used as offset (0 = current bar).
- `Bollinger Deviation` – standard deviation multiplier.

## Position Management

- Only finished candles are processed.
- Positions are closed with market orders in the opposite direction.
- Market orders are sent using `RegisterOrder` to guarantee the correct security is attached.
- Volume respects the security `StepVolume`, `MinVolume`, and `MaxVolume` when available.

## Usage Notes

1. Assign actual `Security` objects before starting the strategy (for example by dragging instruments from the terminal). The
   defaults only provide placeholder identifiers.
2. Ensure level1 data subscriptions are available; otherwise the fallback to candle closes will be used.
3. When converting additional symbols you can extend the `SymbolSlot` array or change default parameters inside the constructor.
4. Because the strategy can hold multiple deals per symbol, check broker margin requirements before enabling scaling.
