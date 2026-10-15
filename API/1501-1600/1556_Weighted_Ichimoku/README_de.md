# Gewichtete Ichimoku-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert Ichimoku-Signale zu einem gewichteten Score.
Kauft, wenn der Score den Kaufschwellenwert überschreitet, und steigt aus, wenn der Score unter den Verkaufsschwellenwert fällt.

## Details

- **Einstiegskriterien**: Score >= BuyThreshold
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Score <= SellThreshold oder unter null, wenn Schwellenwert deaktiviert
- **Stops**: Nein
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `Offset` = 26
  - `BuyThreshold` = 60
  - `SellThreshold` = -49
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: Ichimoku
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
