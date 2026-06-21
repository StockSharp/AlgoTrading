# Hardcore FX Ausbruch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Anpassung des MetaTrader-Experten "HardcoreFX". Die Strategie verfolgt ZigZag-Pivot-Hochs und -Tiefs und eröffnet Positionen, wenn der Preis diese durchbricht. Sie wendet feste Stop-Loss- und Take-Profit-Niveaus an und trailed die Position außerdem, um aufgelaufene Gewinne zu schützen.

## Details
- **Einstiegskriterien**: Schluss über dem letzten ZigZag-Hoch für Long; Schluss unter dem letzten ZigZag-Tief für Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss, Take-Profit oder Trailing-Stop ausgelöst.
- **Stops**: Fester Stop-Loss, Take-Profit und Trailing-Stop.
- **Standardwerte**:
  - `ZigzagLength` = 17
  - `StopLoss` = 1400
  - `TakeProfit` = 5400
  - `TrailingStop` = 500
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Stop-Loss, Take-Profit, Trailing-Stop
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
