# EMA-Kreuzung-MACD-Sitzungsstart-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht long, wenn eine schnelle EMA eine langsame EMA nach oben kreuzt und das MACD-Histogramm positiv ist. Sie geht short bei der entgegengesetzten Kreuzung mit negativem Histogramm. Wenn diese Bedingungen bereits bei der ersten Bar einer Handelssitzung erfüllt sind, wird sofort eine Position eröffnet. Positionen werden bei einem entgegengesetzten Kreuzungssignal oder beim Ende der Sitzung geschlossen.

## Details

- **Einstiegskriterien**:
  - Schnelle EMA kreuzt langsame EMA nach oben mit positivem MACD-Histogramm.
  - Oder beim ersten Sitzungsbar, wenn schnelle EMA über langsamer EMA liegt und MACD-Histogramm positiv ist.
- **Ausstiegskriterien**:
  - Entgegengesetzter EMA-Kreuzung oder Sitzungsende.
- **Indikatoren**: EMA, MACD.
- **Typ**: Trendfolge.
- **Zeitrahmen**: 5 Minuten (Standard).
