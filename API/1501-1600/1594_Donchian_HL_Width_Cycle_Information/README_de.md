# Donchian HL-Breite Zyklusinfo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die auf Basis der Donchian-Kanalbreite und Zykluswechseln handelt.

Die Strategie überwacht das Verhältnis der Kerzenextreme zum Donchian-Kanal. Nach einem Abwärtszyklus öffnet das Erreichen des oberen Bandes eine Long-Position. Nach einem Aufwärtszyklus öffnet das Berühren des unteren Bandes eine Short-Position.

## Details

- **Einstiegskriterien**: Zyklustrendwechsel am Donchian-Kanal.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Zyklussignal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 28
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Donchian
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
