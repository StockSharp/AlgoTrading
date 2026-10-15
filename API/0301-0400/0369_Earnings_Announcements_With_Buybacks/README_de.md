# Gewinnankündigungen mit Aktienrückkäufen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Gewinnankündigungen mit Aktienrückkäufen** kauft Aktien mit aktiven Aktienrückkaufprogrammen einige Tage vor ihren Gewinnankündigungen und steigt kurz nach dem Bericht aus.

## Details
- **Einstiegskriterien**: Kauf `DaysBefore` Tage vor den Earnings, wenn das Unternehmen einen aktiven Rückkauf hat.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Verkauf `DaysAfter` Tage nach dem Earnings-Datum.
- **Stops**: Nein.
- **Standardwerte**:
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Event-driven
  - Richtung: Long
  - Indikatoren: Buyback + Calendar
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
