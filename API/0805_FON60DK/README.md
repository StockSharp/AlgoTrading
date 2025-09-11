# FON60DK
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy opens long positions when the Tillson T3 line rises above the Optimized Trend Tracker (OTT) upper band and Williams %R confirms bullish momentum. The position is closed once Tillson T3 drops below the opposite OTT band while Williams %R enters oversold territory.

## Details

- **Entry Conditions**: `T3 > OTT_up` && `Williams %R > -20`
- **Exit Conditions**: `T3_SAT < OTT_dn_SAT` && `Williams %R < -70`
- **Type**: Trend following
- **Indicators**: Tillson T3, OTT, Williams %R
- **Timeframe**: 1 minute (default)
- **Stops**: None
