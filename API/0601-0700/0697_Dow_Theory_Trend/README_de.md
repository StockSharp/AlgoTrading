# Dow-Theorie-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Dow-Theorie-Trend-Strategie verwendet Pivot-Hochs und -Tiefs, um die Trendrichtung zu bestimmen. Die Strategie geht Long, wenn sowohl höhere Hochs als auch höhere Tiefs auftreten, und geht Short, wenn sowohl niedrigere Hochs als auch niedrigere Tiefs entstehen.

## Details

- **Einstiegskriterien**: Höhere Hochs und höhere Tiefs für Long; niedrigere Hochs und niedrigere Tiefs für Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Umgekehrtes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `PivotLookback` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Kursverhalten
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
