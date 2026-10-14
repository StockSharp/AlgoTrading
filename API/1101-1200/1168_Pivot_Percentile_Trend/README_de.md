# Pivot Percentile Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet die Trendstärke auf Perzentilbasis über mehrere Rückblickperioden und kombiniert sie mit einem SuperTrend-Filter. Long-Trades werden eröffnet, wenn die Trendstärke positiv ist und der Kurs oberhalb der SuperTrend-Linie liegt. Short-Trades werden eingegangen, wenn die Trendstärke negativ ist und der Kurs unterhalb des SuperTrend liegt.

## Details

- **Indikatoren**: Perzentilanalyse, SuperTrend
- **Richtung**: Beide
- **Zeitrahmen**: Beliebig

## Berechnung in diesem Beispiel

Sieben Fenster `PercentileLength * 1..7` verwenden High/Low-Quartile (75%/25%, Nearest Rank). Ein Fenster zählt +1, wenn das aktuelle High strikt über dem oberen Quartil liegt, und -1, wenn das aktuelle Low strikt unter dem unteren liegt. Die aktuelle Kerze gehört zu jedem Fenster.

Die Summe ist die Trendstärke. Positive Stärke und Close über dem Preis-SuperTrend eröffnen Long; negative Stärke und Close darunter eröffnen Short. Eine Gegenposition wird vollständig auf `Volume` umgekehrt. Neutralität oder ein abgelehntes Filtersignal halten die Position. Alle Fenster müssen gefüllt sein; es gibt weder eine Signalpause noch einen Schutzstopp.

Standardwerte: `PercentileLength=40`, `AtrPeriod=10`, `AtrMultiplier=3`, `CandleType=5m`. SuperTrend verwendet Wilder-ATR und die Preisbänder der Plattform.
