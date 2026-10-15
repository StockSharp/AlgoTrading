# Anomalous Holonomy Field Theory-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert EMA, RSI, MACD, ATR, Änderungsrate und VWAP-Distanz zu einem zusammengesetzten Signal. Long-Positionen werden eröffnet, wenn das Signal einen benutzerdefinierte Schwellenwert überschreitet, Short-Positionen, wenn es unter den negativen Schwellenwert fällt. Ein ATR-basierter Stop schützt offene Trades.

## Details

- **Einstiegskriterien**:
  - **Long**: Signal ≥ Schwellenwert.
  - **Short**: Signal ≤ −Schwellenwert.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: ATR-Stop.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `SignalThreshold` = 2
  - `CandleType` = 5 Minuten
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
