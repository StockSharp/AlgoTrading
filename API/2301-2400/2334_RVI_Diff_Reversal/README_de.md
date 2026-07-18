# RVI Diff Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt auf Basis der geglätteten Differenz zwischen dem Relative Vigor Index (RVI) und seiner Signallinie.
Sie erkennt Punkte, an denen diese Differenz aufhört zu fallen und beginnt zu steigen, um Long-Positionen einzugehen, und umgekehrt für Short-Positionen.

## Details

- **Einstiegskriterien**: Steigungsumkehr der geglätteten RVI-Differenz
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `RviLength` = 12
  - `SmoothingLength` = 13
  - `CandleType` = 6-Stunden-Kerzen
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RVI, SMA, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: 6H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
