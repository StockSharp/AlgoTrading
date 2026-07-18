# VIDYA Auto-Trading (Umkehr-Logik)-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen Variable Index Dynamic Average (VIDYA) mit breiten ATR-Bändern.
Ein Long-Trade wird eröffnet, wenn der Preis über das obere Band ausbricht, ein Short-Trade, wenn der Preis unter das untere Band ausbricht.

## Details

- **Einstiegskriterien**: Preis kreuzt ATR-Band um VIDYA
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetzter Band-Ausbruch
- **Stops**: Nein
- **Standardwerte**:
  - `VidyaLength` = 10
  - `VidyaMomentum` = 20
  - `BandDistance` = 2
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: VIDYA, ATR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
