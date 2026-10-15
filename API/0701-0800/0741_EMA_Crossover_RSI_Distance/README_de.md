# EMA-Crossover-Strategie mit RSI und Abstand
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet mehrere EMAs und RSI, um Long- und Short-Signale zu erzeugen, und überprüft den Abstand zwischen schnellen EMAs, um die Trendstärke zu bestätigen.

## Details

- **Einstiegskriterien**:
  - EMA5 über EMA13.
  - EMA40 über EMA55.
  - RSI über 50 und über seinem SMA.
  - Abstand zwischen EMA5 und EMA13 über seinem Durchschnitt und EMA40-EMA13-Abstand zunehmend.
  - Schlusskurs über EMA5.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**:
  - Signal wechselt auf neutral oder entgegengesetzte Richtung.
- **Stops**: Nein.
- **Standardwerte**:
  - `EmaShortLength` = 5
  - `EmaMediumLength` = 13
  - `EmaLong1Length` = 40
  - `EmaLong2Length` = 55
  - `RsiLength` = 14
  - `RsiAverageLength` = 14
  - `DistanceLength` = 5
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, RSI
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
