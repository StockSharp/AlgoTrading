# AntiFragile EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grid-Strategie, die geschichtete Limit-Orders ober- und unterhalb des aktuellen Preises mit zunehmendem Volumen platziert.
Positionen werden durch einen anfänglichen Stop geschützt und bei günstiger Kursbewegung mit einem Trailing-Stop verfolgt.

## Details

- **Einstiegskriterien**:
  - Long: Buy-Limit-Orders jeden `SpaceBetweenTrades` Schritte unterhalb des Bid platzieren.
  - Short: Sell-Limit-Orders jeden `SpaceBetweenTrades` Schritte oberhalb des Ask platzieren.
- **Long/Short**: Optional für jede Seite über `TradeLong` und `TradeShort`.
- **Ausstiegskriterien**: Trailing-Stop oder Ausführung der gegenüberliegenden Grid-Seite.
- **Stops**: Anfänglicher `StopLossPips` und Trailing über `TrailingStopPips`.
- **Standardwerte**:
  - `StartingVolume` = 0.1m
  - `IncreasePercentage` = 1m
  - `SpaceBetweenTrades` = 700m
  - `NumberOfTrades` = 50
  - `StopLossPips` = 300m
  - `TrailingStopPips` = 100m
  - `TradeLong` = true
  - `TradeShort` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Grid-Trading
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
