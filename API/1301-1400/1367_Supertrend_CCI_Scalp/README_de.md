# Supertrend & CCI Scalp-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Supertrend & CCI Scalp-Strategie verwendet zwei Supertrend-Linien und einen geglätteten CCI, um kurzfristige Umkehrungen zu erfassen.
Kauft, wenn der erste Supertrend über dem Preis liegt, der zweite unter dem Preis und der geglättete CCI unter -100 fällt. Die Short-Logik spiegelt dieses Setup.

## Details

- **Einstiegskriterien**: Supertrend1 über dem Preis, Supertrend2 unter dem Preis, geglätteter CCI < -100 (Long); umgekehrt für Short
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetzte Supertrend-Ausrichtung oder CCI-Kreuzung von ±100
- **Stops**: Nein
- **Standardwerte**:
  - `AtrLength1` = 14
  - `Factor1` = 3
  - `AtrLength2` = 14
  - `Factor2` = 6
  - `CciLength` = 20
  - `SmoothingLength` = 5
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CciLevel` = 100
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Supertrend, CCI, Moving Average
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

