# TRAX Detrended Price Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategy using TRAX and DPO oscillators to trade trend reversals.

## Details
- **Entry Criteria**: DPO crossing TRAX with TRAX sign and SMA filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover signals.
- **Stops**: None.
- **Default Values**: TRAX Length 12, DPO Length 19, SMA Confirmation Length 3.
- **Filters**: TRAX sign and confirmation SMA.
