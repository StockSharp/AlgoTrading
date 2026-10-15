# Engulfing-mit-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen SuperTrend-Filter mit bullischen und bearischen Engulfing-Mustern. Ein Trade wird eröffnet, wenn eine Kerze die vorherige Bar in Richtung des vorherrschenden Trends engulfiert. Stop- und Ziellevels werden aus dem Musterbereich berechnet.

## Details

- **Einstiegskriterien**: Engulfing-Muster in Übereinstimmung mit der SuperTrend-Richtung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Ja, basierend auf Kerzenextremen und ATR-Versatz.
- **Standardwerte**:
  - `CandleType` = 5 Minuten
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3
  - `BoringThreshold` = 25
  - `EngulfingThreshold` = 50
  - `StopLevel` = 200
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: SuperTrend, Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
