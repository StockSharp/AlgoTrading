# Connors VIX Umkehr III
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konträre Strategie, die VIX-Spitzen relativ zu seinem gleitenden Durchschnitt nutzt. Sie kauft, wenn der VIX um einen festgelegten Prozentsatz über den Durchschnitt springt, und geht short, wenn der VIX darunter fällt.

Positionen werden geschlossen, wenn der VIX den gleitenden Durchschnitt des Vortages kreuzt.

## Details

- **Einstiegskriterien**: VIX-Tief über MA und Schlusskurs über MA um Schwellenwert für Käufe; VIX-Hoch unter MA und Schlusskurs unter Schwellenwert für Verkäufe.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: VIX kreuzt den gestrigen MA.
- **Stops**: Nein.
- **Standardwerte**:
  - `LengthMA` = 10
  - `PercentThreshold` = 10m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Konträr
  - Richtung: Beide
  - Indikatoren: VIX, SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
