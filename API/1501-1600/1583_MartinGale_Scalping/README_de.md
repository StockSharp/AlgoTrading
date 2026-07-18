# MartinGale-Scalping-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Kreuzen von SMA(3) und SMA(8) löst Einstiege mit Martingale-artiger Pyramidisierung aus. Zusätzliche Orders werden in jeder Bar hinzugefügt, bis Stop oder Take-Profit erreicht wird.

## Details

- **Einstiegskriterien**: `SMA3` über `SMA8` für Longs, darunter für Shorts; neue Einstiege werden hinzugefügt, solange das Signal anhält.
- **Long/Short**: Konfigurierbar über `TradingMode`.
- **Ausstiegskriterien**: Preis erreicht `TakeProfit` oder `StopLoss` und entgegengesetzte SMA-Beziehung.
- **Stops**: Ja, basierend auf dem langsamen SMA-Wert.
- **Standardwerte**:
  - `FastLength` = 3
  - `SlowLength` = 8
  - `TakeProfit` = 1.03
  - `StopLoss` = 0.95
  - `TradingMode` = Long
  - `CandleType` = 5 minutes
  - `MaxPyramids` = 5
- **Filter**:
  - Kategorie: Trend
  - Richtung: Konfigurierbar
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
