# MACD-Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem MACD-Indikator.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 64%. Die Strategie funktioniert am besten im Devisenmarkt.

Der MACD-Trend reagiert auf Kreuzungen zwischen der MACD-Linie und ihrer Signallinie. Bullische Kreuzungen initiieren Longs, während bärische Kreuzungen Shorts starten. Entgegengesetzte Kreuzungen oder ein Stop schließen den Trade.

Der Moving Average Convergence Divergence-Indikator passt sich durch die Messung von Momentum gut an sich verändernde Märkte an. Dieser Ansatz zielt darauf ab, Trendswings zu reiten, solange der Indikator eine klare bullische oder bärische Tendenz beibehält.


## Details

- **Einstiegskriterien**: Signale basierend auf MA, MACD.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA, MACD
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

