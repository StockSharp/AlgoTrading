# Phase Cross Strategy with Zone
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This sample strategy enters long when a smoothed SMA with positive offset crosses above an EMA with negative offset. The position is closed when the opposite crossover occurs.

## Details

- **Entry Criteria**: SMA + offset crosses above EMA - offset.
- **Long/Short**: long only.
- **Exit Criteria**: opposite crossover.
- **Stops**: none.
- **Default Values**:
  - `Length` = 20.
  - `Offset` = 0.5.
- **Filters**: none.
- **Complexity**: low.
- **Timeframe**: configurable.

