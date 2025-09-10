# Williams Alligator ATR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Williams Alligator indicator combined with an ATR-based stop-loss. A long position is opened when the Lips line crosses above the Jaw line. The position is closed when the Lips cross below the Jaw or when price falls to an ATR-based stop level.

## Details
- **Entry Criteria**: Lips crossing above Jaw.
- **Exit Criteria**: Lips crossing below Jaw or ATR stop-loss.
- **Indicators**: Smoothed Moving Averages, Average True Range.
- **Type**: Long only.
