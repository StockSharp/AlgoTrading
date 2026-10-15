# Safa Bot Alert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Safa Bot Alert-Strategie verwendet eine kurze SMA mit einem ADX-Filter, um Preiskreuzungen zu handeln. Sie geht Long, wenn der Kurs die SMA von unten kreuzt und die Trendstärke hoch ist, und Short bei Kreuzungen nach unten. Festes Take-Profit, Stop-Loss und ein Trailing-Stop verwalten die Positionen; alle Trades werden zu einer festgelegten Sitzungszeit geschlossen.

## Details

- **Einstiegskriterien**: Kurs kreuzt SMA und ADX > `AdxThreshold`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Take-Profit, Stop-Loss, Trailing-Stop oder Sitzungsschluss.
- **Stops**: Fest und Trailing.
- **Standardwerte**:
  - `SmaLength` = 3
  - `TakeProfitPoints` = 80m
  - `StopLossPoints` = 35m
  - `TrailPoints` = 15m
  - `AdxLength` = 15
  - `AdxThreshold` = 15m
  - `SessionCloseHour` = 16
  - `SessionCloseMinute` = 0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, ADX
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
