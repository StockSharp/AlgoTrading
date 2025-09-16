# MTC Combo v2 Strategy

Converted from MetaTrader script "MTC Combo v2 (barabashkakvn's edition)".

## Logic
- Uses slope of a moving average to determine basic trend.
- Optional perceptron filter calculates weighted sum of recent open-price differences over configurable lags.
- `Pass` parameter selects which perceptron branches are used:
  - 4: require perceptron3 > 0 and perceptron2 > 0 for long; perceptron3 <= 0 and perceptron1 < 0 for short.
  - 3: use perceptron2 > 0 for long.
  - 2: use perceptron1 < 0 for short.
  - other values: trade based only on MA slope.

Stop loss and take profit levels are taken from `Sl*` and `Tp*` parameters.

## Parameters
- `MaPeriod` – moving average length.
- `P2`, `P3`, `P4` – lags for perceptrons.
- `Pass` – decision mode.
- `Sl1`/`Tp1`, `Sl2`/`Tp2`, `Sl3`/`Tp3` – stop and target for each branch.
- `CandleType` – candle series to process.

## Notes
The strategy holds a single position at a time and closes it when stop loss or take profit is reached.

## Disclaimer
For educational use only. No investment advice.
