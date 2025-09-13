# Combo Right Strategy

Implementation of strategy converted from MQL script `combo_right.mq5`.
The system combines a basic CCI signal with three simple perceptrons working on price differences.

## Logic

1. **Basic Signal** – Commodity Channel Index (CCI) value. Positive values favour long trades, negative values favour short trades.
2. **Perceptrons** – Each perceptron looks at a set of shifted closing prices and applies linear weights. The mode parameter `Pass` selects which perceptrons are active:
   - `1`: only basic CCI signal.
   - `2`: sale perceptron can override CCI and open short positions.
   - `3`: buy perceptron can override CCI and open long positions.
   - `4`: general perceptron supervises both buy and sale perceptrons.

If an active perceptron issues a signal, it replaces the basic CCI output. Otherwise the CCI value is used.

## Parameters

- `TakeProfit1`, `StopLoss1` – profit and loss targets for the basic CCI signal (in ticks).
- `CciPeriod` – lookback period of the CCI indicator.
- Weights and periods for each perceptron (`x12`, `x22`, …, `p4`).
- `Pass` – operation mode.
- `Shift` – bar index used for price data (0 current, 1 previous).
- `Volume` – trade volume.
- `CandleType` – candle type for calculations.

## Indicators

- CCI.

