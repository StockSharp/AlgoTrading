# Custom Buy BID-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Custom Buy BID-Strategie nutzt den Supertrend-Indikator, um bullische Umkehrungen zu identifizieren. Sie eröffnet eine Long-Position, wenn der Kurs die Supertrend-Linie von unten kreuzt, und verwendet konfigurierbare Gewinn- und Verlustziele für das Risikomanagement.

## Details

- **Einstiegskriterien**: Kurs kreuzt Supertrend von unten.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Take Profit oder Stop Loss.
- **Stops**: Ja.
- **Standardwerte**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `TakeProfitPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StartDate` = 2018-09-01
  - `EndDate` = 9999-01-01
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: Supertrend
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
