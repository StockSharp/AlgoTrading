# CANX MA-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt EMA-Kreuzungen des Medianpreises (HL2). Eine Long-Position wird eröffnet, wenn die schnelle EMA die langsame EMA von unten kreuzt. Wenn der Nur-Long-Modus deaktiviert ist, wird eine Short-Position eröffnet, wenn die schnelle EMA die langsame von oben kreuzt. Ein Startjahr-Filter verhindert den Handel vor dem angegebenen Jahr.

## Parameter
- Kerzentyp
- Länge der schnellen EMA
- Multiplikator (langsame EMA = schnelle Länge * Multiplikator)
- Nur Long
- Startjahr
