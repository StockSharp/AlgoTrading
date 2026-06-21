# Commitment of Trader R Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Williams %R Indikator zur Erkennung von überkauften und überverkauften Bedingungen. Ein einfacher gleitender Durchschnitt dient als optionaler Trendfilter.

Ein Long-Trade wird eröffnet, wenn Williams %R über den oberen Schwellenwert steigt und der Schlusskurs über der SMA liegt. Ein Short-Trade wird eröffnet, wenn Williams %R unter den unteren Schwellenwert fällt und der Preis unter der SMA liegt. Positionen werden geschlossen, wenn der Oszillator die Signalzone verlässt.

## Details
- **Einstiegskriterien**:
  - **Long**: %R > oberer Schwellenwert und (Preis > SMA wenn aktiviert)
  - **Short**: %R < unterer Schwellenwert und (Preis < SMA wenn aktiviert)
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - **Long**: %R < oberer Schwellenwert
  - **Short**: %R > unterer Schwellenwert
- **Stops**: Nein
- **Standardwerte**:
  - `WilliamsPeriod` = 252
  - `UpperThreshold` = -10
  - `LowerThreshold` = -90
  - `SmaEnabled` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Williams %R, SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
