# AO AC Handelszonen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert das „AO/AC Trading Zones"-Konzept. Sie kombiniert den Awesome Oscillator (AO), Acceleration/Deceleration (AC) und Bill Williams Fraktale, um eine Pyramide von Long-Positionen aufzubauen, wenn der Impuls über die Alligator-Zahnlinie beschleunigt.

## Details

- **Einstieg**: Mindestens zwei aufeinanderfolgende Bars mit `close > teeth`, `AO > AO[1]`, `AC > AC[1]` und `close > EMA`.
- **Pyramidisierung**: Fügt bis zu fünf Long-Positionen hinzu, solange die Bedingungen gültig sind.
- **Ausstieg**: Fraktal-Trendumkehr oder Preisfall unter das Stop-Level.
- **Indikatoren**: SMMA (Zähne), AO, AC, EMA.
- **Stop**: Tief der fünften grünen Bar.
