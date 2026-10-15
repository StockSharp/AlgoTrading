# CCI Automatisiert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

CCI Automatisiert ist eine Umkehrstrategie, die auf Schwellenwert-Kreuzungen des Commodity Channel Index (CCI) reagiert. Sie geht Long, wenn der CCI über −80 steigt, nachdem er unter −90 gefallen war, und Short, wenn der CCI unter 80 fällt, nachdem er 90 überschritten hatte. Das System verdoppelt Trades bis zu einem benutzerdefinierten Limit, verwaltet das Risiko mit festen Take-Profit- und Stop-Loss-Niveaus und verfolgt Gewinne mit einem konfigurierbaren Trailing-Stop.

Der Ansatz zielt darauf ab, frühe Momentum-Verschiebungen nach überkauften oder überverkauften Zuständen zu erfassen. Durch das Aufstocken mehrerer Positionen und das Verschieben des Stops, wenn der Kurs voranschreitet, versucht er von anhaltenden Umkehrungen zu profitieren und gleichzeitig das Verlustrisiko zu begrenzen.

## Details

- **Einstiegskriterien**: CCI kreuzt über -80, nachdem er unter -90 war, für Longs; kreuzt unter 80, nachdem er über 90 war, für Shorts.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss, Take-Profit oder Trailing-Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `CciPeriod` = 9
  - `TradesDuplicator` = 3
  - `Volume` = 0.03
  - `StopLoss` = 50
  - `TakeProfit` = 200
  - `TrailingStop` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: CCI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
