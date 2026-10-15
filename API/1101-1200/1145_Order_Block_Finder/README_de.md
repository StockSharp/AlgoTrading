# Order-Block-Finder-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie identifiziert bullische und bärische Order Blocks anhand einer bestimmten Anzahl aufeinanderfolgender Kerzen und einer Mindestprozentualen Bewegung. Wird ein bullischer Order Block erkannt, kauft die Strategie; bei einem bärischen Block verkauft sie.

## Parameter
- **Relevant Periods** – Anzahl der nachfolgenden Kerzen zur Bestätigung eines Order Blocks
- **Min Percent Move** – minimale prozentuale Veränderung zwischen dem Block und der letzten Bestätigungskerze
- **Use Whole Range** – High/Low-Bereich anstelle von Open-basierten Grenzen verwenden
- **Candle Type** – Kerzentyp für Berechnungen
