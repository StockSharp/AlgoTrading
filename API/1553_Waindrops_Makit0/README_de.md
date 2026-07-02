# Strategie Waindrops Makit0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Vereinfachte Strategie, die den VWAP zweier Hälften eines benutzerdefinierten Zeitraums vergleicht.

## Details

- **Einstiegskriterien**: VWAP der rechten Hälfte gegenüber VWAP der linken Hälfte.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal.
- **Stops**: Nein.
- **Standardwerte**:
  - `PeriodMinutes` = 60
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Beide
  - Indikatoren: VWAP
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
