# Nifty 50 5-Minuten-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Nifty 50 5-Minuten-Strategie** handelt Ausbrüche im Nifty-50-Index mit Bestätigung durch DEMA, VWAP und Bollinger-Bänder.

## Details
- **Einstiegskriterien**:
  - **Long**: Schlusskurs über dem vorherigen Hoch, über dem oberen Bollinger-Band und DEMA über VWAP.
  - **Short**: Schlusskurs unter dem vorherigen Tief, unter dem unteren Bollinger-Band und DEMA unter VWAP.
- **Long/Short**: beide.
- **Ausstiegskriterien**: Stop-Loss.
- **Stops**: Ja, feste Punkte.
- **Standardwerte**:
  - `DemaPeriod = 6`
  - `BollingerLength = 20`
  - `BollingerStdDev = 2`
  - `LookbackPeriod = 5`
  - `StopLossPoints = 25`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: DEMA, VWAP, Bollinger Bands
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
