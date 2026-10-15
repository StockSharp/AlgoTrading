# Zero-Lag TEMA Crosses-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zero-Lag-Triple-EMA-Kreuzsystem. Positionen verwenden aktuelle Hochs und Tiefs für Stops sowie risikobasierte Kursziele.

## Details

- **Einstiegskriterien**: Schnelle TEMA kreuzt langsame TEMA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop am letzten Extrempunkt oder Ziel per Verhältnis.
- **Stops**: Ja.
- **Standardwerte**:
  - `Lookback` = 20
  - `FastPeriod` = 69
  - `SlowPeriod` = 130
  - `RiskReward` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: TEMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
