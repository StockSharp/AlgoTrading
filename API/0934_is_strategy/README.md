# IS Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A simple strategy that opens a long position when the selected source equals the long trigger value and closes it when the value switches to the opposite. If short selling is enabled, the strategy also opens a short position on the opposite signal. Take profit and stop loss are specified as percentages of the entry price.

## Details

- **Entry Criteria**:
  - **Long**: Source equals the long value and previous value was different.
  - **Short**: Source equals the short value and previous value was different (if shorts enabled).
- **Exit Criteria**: Reverse signal or protective stop.
- **Stops**: Yes, take-profit and stop-loss as percentages.
