# EMA MACD Signal Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht Long, wenn der schnelle EMA über dem langsamen EMA liegt und die MACD-Signallinie steigt. Sie geht Short, wenn der schnelle EMA unter dem langsamen EMA liegt und die Signallinie fällt. Stop-Loss, Take-Profit und Trailing Stop sind optional.

## Details

- **Einstiegskriterien**:
  - Schneller EMA > Langsamer EMA und MACD-Signal steigt → Kaufen.
  - Schneller EMA < Langsamer EMA und MACD-Signal fällt → Verkaufen.
- **Ausstiegskriterien**:
  - Das entgegengesetzte Einstiegssignal schließt die Position.
- **Indikatoren**: EMA, MACD signal.
- **Typ**: Trendfolge.
- **Zeitrahmen**: 5 Minuten (Standard).
