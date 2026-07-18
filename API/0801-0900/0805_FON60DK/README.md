# FON60DK
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy opens long positions when the Tillson T3 line rises above the Optimized Trend Tracker (OTT) upper band and Williams %R confirms bullish momentum. The position is closed once Tillson T3 drops below the opposite OTT band while Williams %R enters oversold territory.

## Details

- **Entry Conditions**: `T3 > OTT_up` && `Williams %R > -20`
- **Exit Conditions**: `T3_SAT < OTT_dn_SAT` && `Williams %R < -70`
- **Type**: Trend following
- **Indicators**: Tillson T3, OTT, Williams %R
- **Timeframe**: 1 minute (default)
- **Stops**: None
