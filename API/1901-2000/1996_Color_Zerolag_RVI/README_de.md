# Color Zerolag RVI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Relative Vigor Index und seine Signallinie.
Sie kauft, wenn die Haupt-RVI-Linie die Signallinie von oben nach unten kreuzt, und verkauft, wenn die Hauptlinie die Signallinie von unten nach oben kreuzt.

## Details

- **Einstiegskriterien**: Kreuzung von RVI und Signallinie
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `RviLength` = 14
  - `SignalLength` = 9
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = 4 Stunden
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RVI, SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (H4)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
