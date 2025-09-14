# TEMA Custom Slope Strategy

Reversal strategy using slope changes of a Triple Exponential Moving Average (TEMA). The indicator is calculated on the specified timeframe and the strategy reacts to direction shifts.

## How It Works

- **Entry Criteria**:
  - **Long**: TEMA was falling and turns upward.
  - **Short**: TEMA was rising and turns downward.
- **Exit Criteria**: reverse signal closes the existing position.
- **Indicators**: Triple Exponential Moving Average.

## Key Parameters

- `TemaLength` – Number of bars for the TEMA calculation.
- `CandleType` – Timeframe of candles used for analysis.
