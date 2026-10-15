# Trendfolge-Strategie Parabolic Kauf Verkauf
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert Parabolic SAR mit gleitenden Durchschnittkreuzungen.
Long-Einstiege erfolgen, wenn der Preis über einem langfristigen Trend-SMA liegt, der schnelle EMA den langsamen EMA kreuzt und der SAR bullish ist.
Short-Einstiege verwenden die entgegengesetzten Bedingungen.
Der Stop Loss wird beim Trend-SMA platziert und der Take Profit verwendet ein Risiko/Ertrag-Verhältnis.

## Details

- **Einstieg**:
  - **Long**: Preis > Trend-SMA, schneller EMA kreuzt über langsamen EMA, SAR bullish
  - **Short**: Preis < Trend-SMA, schneller EMA kreuzt unter langsamen EMA, SAR bearish
- **Ausstieg**:
  - Stop beim Trend-SMA
  - Take Profit = Risiko/Ertrag * Abstand vom Einstieg zum Trend-SMA
- **Indikatoren**: Parabolic SAR, SMA, EMA
- **Zeitrahmen**: konfigurierbar
- **Typ**: Trendfolge
