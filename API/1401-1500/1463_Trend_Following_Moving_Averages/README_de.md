# Trendfolge-Strategie mit gleitenden Durchschnitten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Berechnet einen gleitenden Durchschnitt und misst dessen Trend innerhalb eines dynamischen Preiskanals.
Long-Positionen werden bei positivem Trend-Score und Short-Positionen bei negativem Trend-Score eingegangen.

## Details

- **Einstieg**:
  - **Long**: Trend-Score > 0
  - **Short**: Trend-Score < 0
- **Ausstieg**: umgekehrtes Signal
- **Indikatoren**: SMA, Highest, Lowest
- **Zeitrahmen**: konfigurierbar
- **Typ**: Trendfolge
