# Autonomer 5-Minuten-Roboter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Autonome 5-Minuten-Roboter-Strategie handelt auf einem 5-Minuten-Zeitrahmen.
Sie geht long, wenn der Preis im Aufwärtstrend liegt und der Kaufdruck den Verkaufsdruck übersteigt,
und geht short bei entgegengesetzten Bedingungen.

## Details

- **Einstiegskriterien**: Aufwärtstrend (Schluss über dem 50-Perioden-SMA und über dem Schluss vor 6 Bars) mit Kaufvolumen größer als Verkaufsvolumen.
- **Ausstiegskriterien**: Positionsumkehr bei entgegengesetztem Signal.
- **Stops**: 3% Stop-Loss und 29% Take-Profit vom Einstiegspreis.
- **Standardwerte**:
  - `MaLength` = 50
  - `VolumeLength` = 10
  - `StopLossPercent` = 3
  - `TakeProfitPercent` = 29
  - `CandleType` = 5m
