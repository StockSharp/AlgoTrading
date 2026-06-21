# Crypto-Volatilität Bitcoin-Korrelation
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Long-Position, wenn die Bitcoin-Volatilität zusammen mit dem BVOL7D-Index steigt und der Preis über seiner EMA notiert. Sie schließt, wenn der Preis wieder unter die EMA fällt.

## Details

- **Einstiegskriterien**: VIXFix größer als Vorwert, BVOL7D größer als Vorwert, Schlusskurs über EMA.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Schlusskurs unter EMA.
- **Stops**: Nein.
- **Standardwerte**:
  - `VixFixLength` = 22
  - `EmaLength` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Nur Long
  - Indikatoren: Highest, EMA
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
