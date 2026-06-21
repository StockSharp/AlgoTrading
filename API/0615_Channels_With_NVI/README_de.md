# Kanal-Strategie mit NVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert Bollinger Bands oder Keltner Channels mit dem Negative Volume Index (NVI). Eine Long-Position wird eröffnet, wenn der Preis unterhalb des unteren Bands schließt und der NVI über seiner EMA liegt. Die Position wird geschlossen, wenn der NVI unter seine EMA fällt. Optionale Stop-Loss- und Take-Profit-Prozentsätze sind verfügbar.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs < unteres Band und NVI > NVI-EMA.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - **Long**: NVI < NVI-EMA.
- **Stops**: Optional, Prozent des Einstiegspreises.
- **Standardwerte**:
  - `ChannelType` = "BB"
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
  - `NviEmaLength` = 200
  - `EnableStopLoss` = false
  - `StopLossPercent` = 0
  - `EnableTakeProfit` = false
  - `TakeProfitPercent` = 0
- **Filter**:
  - Kategorie: Kanal
  - Richtung: Nur Long
  - Indikatoren: Bollinger Bands oder Keltner Channels, EMA, NVI
  - Stops: Optional
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
