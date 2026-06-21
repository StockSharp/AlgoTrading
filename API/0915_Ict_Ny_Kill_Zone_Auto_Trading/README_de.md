# ICT NY Kill Zone Auto Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die während der New Yorker Kill Zone mithilfe von Fair Value Gaps und Order Blocks handelt.

## Details

- **Einstiegskriterien**: Fair Value Gap und Order Block innerhalb der Kill Zone.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Positionsschutz.
- **Stops**: Ja.
- **Standardwerte**:
  - `StopLoss` = 30
  - `TakeProfit` = 60
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Price Action
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

