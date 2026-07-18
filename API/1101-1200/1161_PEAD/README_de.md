# PEAD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt den Post-Earnings Announcement Drift nach einer positiven EPS-Überraschung und einem Gap nach oben.
Sie geht am Tag nach den Earnings long, wenn der Kurs mit einem Gap nach oben öffnet und die jüngste Entwicklung positiv ist,
und verwendet einen EMA-Trailing, einen festen Stop/Breakeven und eine maximale Haltedauer.

## Details

- **Einstiegskriterien**: Positive EPS-Überraschung, Gap nach oben nach den Earnings und positive Vorperformance.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Täglicher EMA-Crossunder, fester Stop/Breakeven oder maximale Haltebars.
- **Stops**: Fester Stop mit Breakeven.
- **Standardwerte**:
  - `GapThreshold` = 1
  - `EpsSurpriseThreshold` = 5
  - `PerfDays` = 20
  - `StopPct` = 8
  - `EmaLen` = 50
  - `MaxHoldBars` = 50
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Earnings
  - Richtung: Long
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
