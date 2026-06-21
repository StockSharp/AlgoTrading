# Hurst Future Lines of Demarcation Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet eine geglättete Future Line of Demarcation (FLD) und drei Zykluslängen (Signal, Trade, Trend). Sie tritt ein, wenn der Preis die Signal-FLD in bestimmten Trendzuständen kreuzt, und tritt aus, wenn ausgewählte Werte sich kreuzen.

## Details

- **Einstiegskriterien**:
  - Kaufen, wenn der Preis die Signal-FLD nach oben kreuzt, während der Trendzustand gleich 1 ist.
  - Verkaufen, wenn der Preis die Signal-FLD nach unten kreuzt, während der Trendzustand gleich 6 ist.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Position schließen, wenn `CloseTrigger1` `CloseTrigger2` in entgegengesetzter Richtung des Trades kreuzt.
- **Stops**: Nein.
- **Standardwerte**:
  - `SmoothFld` = false
  - `FldSmoothing` = 5
  - `SignalCycleLength` = 5
  - `TradeCycleLength` = 20
  - `TrendCycleLength` = 80
  - `CloseTrigger1` = Price
  - `CloseTrigger2` = Trade
