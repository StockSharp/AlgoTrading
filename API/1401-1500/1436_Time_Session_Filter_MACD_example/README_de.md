# Zeitsitzungsfilter - MACD-Beispiel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die die Verwendung eines Zeitsitzungsfilters mit MACD und Trend-EMA demonstriert. Handelt nur während der konfigurierten Stunden.

## Details

- **Einstiegskriterien**: MACD kreuzt Signal innerhalb der aktiven Sitzung und Preis relativ zur Trend-EMA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenläufige Kreuzung oder Sitzungsende, wenn aktiviert.
- **Stops**: Nein.
- **Standardwerte**:
  - `SessionStart` = 11:00
  - `SessionEnd` = 15:00
  - `CloseAtSessionEnd` = false
  - `FastEmaPeriod` = 11
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `TrendMaLength` = 55
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD, EMA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
