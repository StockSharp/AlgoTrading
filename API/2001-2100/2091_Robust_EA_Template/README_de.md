# Robust EA-Vorlage-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die die Robust EA-Vorlage aus MQL implementiert.
Sie verwendet den Commodity Channel Index (CCI) und den Relative Strength Index (RSI), um Einstiegssignale zu generieren, und wendet festen Take-Profit und Stop-Loss an.

## Logik
- Kaufen, wenn CCI bei -200..-150 oder -100..-50 liegt und RSI zwischen 0 und 25 ist.
- Verkaufen, wenn CCI zwischen 50 und 150 liegt und RSI zwischen 80 und 100 ist.
- Stop-Loss und Take-Profit werden in Pips definiert und in Preispunkte umgerechnet.

## Parameter
- `Candle Type` – Kerzen-Datenserie.
- `CCI Period` – Periode des CCI-Indikators.
- `RSI Period` – Periode des RSI-Indikators.
- `Take Profit (pips)` – Abstand für das Gewinnziel.
- `Stop Loss (pips)` – Abstand für den Stop-Loss.
- `Volume` – Auftragsvolumen.
