# Mean Reversion with Incremental Entry Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters trades when price deviates from a simple moving average by a defined percentage. Additional orders are placed incrementally as price moves further away from the average.

Positions are closed once price returns to the moving average.

## Details

- **Entry Criteria:**
  - **Long:** `Low < SMA` and percent difference between `Low` and `SMA` ≥ `Initial Percent`.
  - **Short:** `High > SMA` and percent difference between `High` and `SMA` ≥ `Initial Percent`.
- **Incremental Entries:** New orders are added every `Percent Step` further from the previous entry.
- **Exit Criteria:**
  - **Long:** `Close ≥ SMA`.
  - **Short:** `Close ≤ SMA`.
- **Indicators:** SMA.
- **Default Values:**
  - `MA Length` = 30.
  - `Initial Percent` = 5.
  - `Percent Step` = 1.
