# Reine Preis-Aktions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Vereinfachte Preis-Aktions-Strategie, die Break of Structure (BOS) und Market Structure Shift (MSS) anhand jüngster Hochs und Tiefs erkennt.

Die Strategie geht bei BOS long und bei MSS short, mit festen prozentualen Stop-Loss- und Take-Profit-Werten.

## Details

- **Einstiegskriterien**: BOS für Long, MSS für Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Fester Prozentsatz.
- **Standardwerte**:
  - `Length` = 5
  - `SlPercent` = 1m
  - `TpPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
