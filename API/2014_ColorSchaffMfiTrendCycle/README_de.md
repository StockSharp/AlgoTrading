# Color Schaff MFI Trend-Zyklus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Übersetzung des MQL5-Experten `Exp_ColorSchaffMFITrendCycle`.
Sie verwendet den Indikator **Color Schaff MFI Trend Cycle**, der
Money Flow Index-Werte mit einer doppelten stochastischen Berechnung kombiniert. Der Indikator
erzeugt acht Farbzustände, die Momentum- und überkaufte/überverkaufte Zonen darstellen.

Handelslogik:

- Wenn die vorherige Indikatorfarbe **grün** ist (Indizes 6-7) und die aktuelle
  Farbe unter die starke Aufwärtstrendzone fällt, schließt die Strategie Short-Positionen
  und eröffnet eine neue Long-Position.
- Wenn die vorherige Indikatorfarbe **orange** ist (Indizes 0-1) und die aktuelle
  Farbe über die starke Abwärtstrendzone steigt, schließt die Strategie Long-Positionen
  und eröffnet eine neue Short-Position.

Parameter:

- `FastMfiPeriod` – Periode des schnellen MFI.
- `SlowMfiPeriod` – Periode des langsamen MFI.
- `CycleLength` – Länge des zyklischen Puffers im Indikator.
- `HighLevel` / `LowLevel` – Überkauf- und Überverkauf-Schwellenwerte für den STC-Wert.
- `CandleType` – Zeitrahmen der Eingabekerzen (Standard 1 Stunde).

Die Strategie verwendet die High-Level-API von StockSharp und verarbeitet nur abgeschlossene Kerzen.
