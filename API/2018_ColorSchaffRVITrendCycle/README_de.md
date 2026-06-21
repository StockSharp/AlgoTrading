# Color Schaff RVI Trend-Zyklus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert den Color Schaff RVI Trend Cycle mit der High-Level-API von StockSharp. Der Indikator wendet einen doppelten stochastischen Prozess auf die Differenz zwischen schnellen und langsamen Relative Vigor Index-Werten an und glättet das Ergebnis.

## Parameter
- `FastRviLength` – Periode für die schnelle RVI-Berechnung (Standard 23).
- `SlowRviLength` – Periode für die langsame RVI-Berechnung (Standard 50).
- `CycleLength` – Länge der stochastischen Zyklen (Standard 10).
- `HighLevel` – oberer Schwellenwert zur Erkennung bullischer Bedingungen (Standard 60).
- `LowLevel` – unterer Schwellenwert zur Erkennung bärischer Bedingungen (Standard -60).
- `CandleType` – von der Strategie verarbeiteter Kerzentyp (Standard 4-Stunden-Zeitrahmen).

## Handelslogik
1. Schnelle und langsame RVI-Werte berechnen.
2. Den Schaff Trend Cycle aus der RVI-Differenz aufbauen.
3. **Kaufen**, wenn der STC-Wert über dem oberen Niveau liegt und steigt.
4. **Verkaufen**, wenn der STC-Wert unter dem unteren Niveau liegt und fällt.

## Hinweise
- Die Strategie verarbeitet nur abgeschlossene Kerzen.
- Positionsschutz wird beim Start aktiviert.
- Dieses Beispiel dient nur zu Bildungszwecken und stellt keine Finanzberatung dar.
