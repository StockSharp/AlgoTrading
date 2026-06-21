# Internal Bar Strength IBS Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Short-Mean-Reversion-Strategie unter Verwendung der Internal Bar Strength (IBS). Geht short, wenn IBS hoch ist und der Preis über das vorherige Hoch ausbricht; schließt die Position, wenn IBS unter einen unteren Schwellenwert fällt.

## Details

- **Einstiegskriterien**: IBS >= oberer Schwellenwert und Schlusskurs > vorheriges Hoch
- **Long/Short**: Short
- **Ausstiegskriterien**: IBS <= unterer Schwellenwert
- **Stops**: Nein
- **Standardwerte**:
  - `UpperThreshold` = 0.9
  - `LowerThreshold` = 0.3
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Short
  - Indikatoren: IBS
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
