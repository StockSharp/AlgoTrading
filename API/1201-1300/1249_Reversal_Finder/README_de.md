# Umkehr-Finder-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Umkehr-Finder sucht nach Kerzen mit großer Spanne, die neue Extremwerte bilden und dann auf die andere Seite der Kerze zurückschließen.
Er kauft, wenn der Preis auf ein neues Tief fällt, aber nahe dem Hoch schließt, und verkauft, wenn der Preis auf ein neues Hoch steigt, aber nahe dem Tief schließt.

## Details

- **Einstiegskriterien**: Rangeerweiterung mit Schlusskurs nahe dem gegenüberliegenden Extremwert nach neuem Hoch/Tief
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `Lookback` = 20
  - `SmaLength` = 20
  - `RangeMultiple` = 1.5
  - `RangeThreshold` = 0.5
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: SMA, Highest, Lowest
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

