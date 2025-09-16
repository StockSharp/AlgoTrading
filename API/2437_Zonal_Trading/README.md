# Zonal Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Zonal Trading strategy replicates Bill Williams' classic "zone" concept. It monitors the color of the Awesome Oscillator (AO) and the Accelerator Oscillator (AC). A green bar means the oscillator value increased compared to the previous bar, while a red bar means it decreased. When both oscillators turn green the strategy opens a long position. When both turn red it opens a short position. Any opposite color closes existing positions.

## Details
- **Entry Criteria**:
  - **Long**: AO increases and AC increases.
  - **Short**: AO decreases and AC decreases.
- **Exit Criteria**:
  - **Long**: AO or AC decreases.
  - **Short**: AO or AC increases.
- **Stops**: none by default.
- **Parameters**:
  - `AoCandleType` – timeframe for the Awesome Oscillator (`H4` by default).
  - `AcCandleType` – timeframe for the Accelerator Oscillator (`H4` by default).
  - `BuyOpen`, `SellOpen` – enable or disable long and short entries.
  - `BuyClose`, `SellClose` – enable or disable exits for long and short positions.
- **Indicators**: Awesome Oscillator (5/34), Accelerator Oscillator (AO minus SMA(5)).
- **Type**: momentum follow, works on any market and timeframe where the oscillators are available.
