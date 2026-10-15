# Table to Filter Trades Per Day Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Simple moving-average crossover strategy using SMA50 and SMA200 with fixed profit and loss targets.

## Details

- **Entry**
  - Long: SMA50 crosses above SMA200.
  - Short: SMA50 crosses below SMA200.
- **Exit**: close position when target or stop is hit.
