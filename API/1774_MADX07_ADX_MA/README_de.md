# MADX-07 ADX MA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wurde vom MQL4-Experten MADX-07 konvertiert. Sie handelt auf H4-Kerzen und kombiniert zwei gleitende Durchschnitte mit dem Average Directional Index (ADX) als Filter.

## Logik

- Long-Einstieg: Preis über der langsamen MA, schnelle MA über der langsamen MA, Preis mindestens `MaDifference` Punkte über der schnellen MA für die letzten zwei Kerzen, ADX steigt über `AdxMainLevel` mit steigendem +DI und fallendem -DI.
- Short-Einstieg: Spiegelbedingungen.
- Die Position wird geschlossen, wenn der Gewinn in Punkten `CloseProfit` erreicht oder wenn eine Limit-Order auf `TakeProfit`-Abstand ausgeführt wird.

## Parameter

- `BigMaPeriod` (25) – Periode der langsameren MA.
- `BigMaType` – Typ der langsameren MA.
- `SmallMaPeriod` (5) – Periode der schnelleren MA.
- `SmallMaType` – Typ der schnelleren MA.
- `MaDifference` (5) – minimaler Abstand zwischen Preis und schneller MA in Punkten.
- `AdxPeriod` (11) – ADX-Berechnungsperiode.
- `AdxMainLevel` (13) – minimaler ADX-Wert.
- `AdxPlusLevel` (13) – minimaler +DI-Wert.
- `AdxMinusLevel` (14) – minimaler -DI-Wert.
- `TakeProfit` (299) – Take-Profit-Abstand in Punkten.
- `CloseProfit` (13) – Gewinn in Punkten für frühzeitigen Ausstieg.
- `Volume` (0.1) – Handelsvolumen.
- `CandleType` – Kerzen-Zeitrahmen (Standard H4).
