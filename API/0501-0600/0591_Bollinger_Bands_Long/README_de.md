# Bollinger Bands Long-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft, wenn der Preis unter das untere Bollinger Band schließt und der RSI überverkauft ist. Die Long-Position wird geschlossen, sobald der Preis zum mittleren Band zurückkehrt.

## Details

- **Einstiegskriterien**:
  - Der Preis schließt unter dem unteren Bollinger Band.
  - RSI unterhalb des Überverkauft-Niveaus.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Der Preis schließt auf oder über dem mittleren Bollinger Band.
- **Stops**: Nein.
- **Standardwerte**:
  - `BbLength` = 10
  - `BbDeviation` = 2
  - `RsiLength` = 14
  - `RsiOversold` = 30
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: Bollinger Bands, RSI
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
