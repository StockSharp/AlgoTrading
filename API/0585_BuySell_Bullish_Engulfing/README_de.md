# Kauf- und Verkaufs-Strategie mit Bullish Engulfing-Muster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Long-Position, wenn eine bullische Kerze den vorherigen bärischen Balken vollständig umschließt und optionale Trendbedingungen erfüllt sind. Die Positionsgröße ist ein Prozentsatz des aktuellen Eigenkapitals, während Take-Profit und Stop-Loss Trades automatisch schließen.

## Details

- **Einstiegskriterien**: Bullish Engulfing-Muster mit optionalem SMA-Trendfilter.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss.
- **Stops**: Ja, sowohl Take-Profit als auch Stop-Loss.
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
  - `TrendMode` = SMA50
- **Filter**:
  - Kategorie: Muster
  - Richtung: Nur Long
  - Indikatoren: Candlestick, SMA
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
