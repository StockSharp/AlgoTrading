# MACD-Histogramm-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das MACD-Histogramm stellt die Differenz zwischen der MACD-Linie und ihrer Signallinie dar. Kreuzungen ober- oder unterhalb von null markieren oft Schwungwechsel. Diese Strategie handelt diese Nulllinien-Kreuzungen und verwaltet das Risiko mit einem prozentualen Stop.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 160%. Am besten funktioniert die Strategie auf dem Devisenmarkt.

Auf jeder Kerze wird das MACD-Histogramm berechnet. Wenn es von negativ auf positiv wechselt, wird eine Long-Position eröffnet. Ein Wechsel von positiv auf negativ löst einen Short-Verkauf aus. Da die Strategie nur nach dem Null-Übergang sucht, sind Trades unkompliziert und typischerweise kurzfristig.

Stops werden verwendet, um Verluste zu begrenzen, wenn der Schwung nicht in der erwarteten Richtung anhält.

## Details

- **Einstiegskriterien**: MACD-Histogramm kreuzt die Nulllinie.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

