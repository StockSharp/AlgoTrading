# Swing FX Pro Panel v1
[Русский](README_ru.md) | [中文](README_cn.md)

Demonstration strategy using an EMA crossover with basic performance statistics. Fast EMA crossing above the slow EMA opens a long position, while a cross below opens a short. Each trade uses fixed profit and loss targets.

## Details

- **Indicators**: EMA
- **Parameters**:
  - `Initial Capital` – starting account size for statistics.
  - `Risk Per Trade` – percentage risk per trade (informational).
  - `Analysis Period` – period length used for analysis.
  - `Fast Length` – fast EMA period.
  - `Slow Length` – slow EMA period.
  - `Profit Target` – profit in price units.
  - `Stop Loss` – loss in price units.

