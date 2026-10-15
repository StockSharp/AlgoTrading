# Gewinnankündigungs-Prämien-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Gewinnankündigungs-Prämien-Strategie** kauft Aktien einige Tage vor Gewinnankündigungen und steigt kurz nach der Veröffentlichung aus.

## Details
- **Einstiegskriterien**: Kauf `DaysBefore` Tage vor den Earnings.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Verkauf `DaysAfter` Tage nach den Earnings.
- **Stops**: Nein.
- **Standardwerte**:
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Event-driven
  - Richtung: Long
  - Indikatoren: Calendar
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
