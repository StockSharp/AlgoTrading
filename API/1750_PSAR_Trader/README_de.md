# PSAR Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die PSAR Trader-Strategie reagiert auf Wechsel des Parabolic SAR-Indikators. Wenn der SAR unter den Preis fällt, wird eine Long-Position eröffnet; wenn der SAR über den Preis steigt, wird eine Short-Position eröffnet. Eine optionale Einstellung "Close On Opposite" kehrt die Position um, wenn ein entgegengesetztes Signal erscheint. Der Handel findet nur während der konfigurierten Sitzungsstunden statt. Stop-Loss und Take-Profit werden durch das Schutzmodul verwaltet.

## Details

- **Einstiegskriterien**: Preis, der den Parabolic SAR kreuzt.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter SAR-Kreuzung oder Positionsumkehr.
- **Stops**: Ja, fest über Parameter.
- **Standardwerte**:
  - `SarStep` = 0.001m
  - `SarMaxStep` = 0.2m
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `TakeValue` = 50 (absolute)
  - `StopValue` = 50 (absolute)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Parabolic SAR
  - Stops: Fest
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
