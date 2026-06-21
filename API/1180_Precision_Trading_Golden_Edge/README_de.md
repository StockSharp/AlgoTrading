# Präzisionshandels-Strategie: Golden Edge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Scalping-Strategie für Gold richtet einen schnellen EMA- und langsamen EMA-Crossover an der Richtung eines Hull Moving Average aus. Trades erfolgen nur, wenn der RSI das Momentum bestätigt und die Volatilität ausreichend ist.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller EMA kreuzt über langsamen EMA, RSI > 55, HMA steigt, Volatilitätsfilter bestanden.
  - **Short**: Schneller EMA kreuzt unter langsamen EMA, RSI < 45, HMA fällt, Volatilitätsfilter bestanden.
- **Indikatoren**: EMA, HMA, RSI, ATR, Highest/Lowest.
- **Typ**: Trendfolge.
- **Zeitrahmen**: Kurzfristig.
