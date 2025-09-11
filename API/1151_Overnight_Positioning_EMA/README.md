# Overnight Positioning with EMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Enters a long position shortly before the selected market closes and exits after the market opens. An optional EMA filter confirms entries. The strategy supports US, Asian, and European sessions and closes any open position before the weekend.

## Details

- **Entry**: Minutes before market close when price is above EMA (if enabled).
- **Exit**: After market open for the specified minutes or five minutes before Friday close.
- **Market**: US, Asia, or Europe.
- **Indicator**: EMA.
- **Direction**: Long only.
- **Stops**: None.
