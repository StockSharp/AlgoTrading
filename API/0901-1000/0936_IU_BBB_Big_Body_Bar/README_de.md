# IU BBB Große-Kerzenkörper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie steigt ein, wenn der Körper der aktuellen Kerze mehrfach größer als die durchschnittliche Körpergröße der letzten 20 Kerzen ist. Eine große bullische Kerze eröffnet eine Long-Position, während eine große bärische Kerze eine Short-Position eröffnet. Positionen werden mit einem ATR-basierten Trailing-Stop abgesichert.

## Details

- **Einstiegskriterien**:
  - **Long**: Körper > durchschnittlicher Körper * BigBodyThreshold und Schluss > Eröffnung.
  - **Short**: Körper > durchschnittlicher Körper * BigBodyThreshold und Schluss < Eröffnung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: ATR-Trailing-Stop.
- **Stops**: Trailing-Stop mit ATR * AtrFactor.
- **Standardwerte**:
  - `BigBodyThreshold` = 4
  - `AtrLength` = 14
  - `AtrFactor` = 2
  - `CandleType` = 5 minute
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: SMA, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

