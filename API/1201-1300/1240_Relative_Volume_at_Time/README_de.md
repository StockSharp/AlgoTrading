# Relatives Volumen zur Tageszeit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die das Volumen zu einer bestimmten Tageszeit mit dem Durchschnittsvolumen der jüngsten Kerzen vergleicht.

## Details

- **Einstiegskriterien**: relatives Volumen über dem Schwellenwert zur angegebenen Tageszeit.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: relatives Volumen fällt wieder unter 1.
- **Stops**: Nein.
- **Standardwerte**:
  - `Period` = 5
  - `Threshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TargetHour` = 9
  - `TargetMinute` = 30
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Beide
  - Indikatoren: SMA, Volume
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
