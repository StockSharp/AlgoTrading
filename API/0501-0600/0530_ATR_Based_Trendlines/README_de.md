# ATR-basierte Trendlinien-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die ATR-basierte Trendlinien aus Pivot-Punkten erstellt und deren Ausbrüche handelt.

## Details

- **Einstiegskriterien**: Ausbruch aus ATR-basierten Trendlinien.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Ausbruch.
- **Stops**: Keine.
- **Standardwerte**:
  - `LookbackLength` = 30
  - `AtrPercent` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, Price Action
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
