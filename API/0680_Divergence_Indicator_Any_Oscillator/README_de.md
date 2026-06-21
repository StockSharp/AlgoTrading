# Divergenzindikator (Beliebiger Oszillator)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Erkennt reguläre und versteckte Divergenzen zwischen Preis und RSI. Die Strategie kauft bei bullischen Divergenzen und verkauft bei bärischen Divergenzen.

## Parameter
- **Pivot Left** – Balken links vom Pivot
- **Pivot Right** – Balken rechts vom Pivot
- **Min Range** – Mindestanzahl von Balken zwischen Pivots
- **Max Range** – Maximale Anzahl von Balken zwischen Pivots
- **RSI Length** – RSI-Periode
- **Candle Type** – Kerzenreihentyp

## Indikator
- RSI

## Regeln
- **Einstieg**:
  - Kaufen, wenn der Preis ein tieferes Tief bildet, während der RSI ein höheres Tief bildet (bullische Divergenz)
  - Verkaufen, wenn der Preis ein höheres Hoch bildet, während der RSI ein niedrigeres Hoch bildet (bärische Divergenz)
  - Versteckte Divergenzen handeln in die entgegengesetzte Richtung
