# Averaging Down-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Position, wenn der Preis außerhalb eines ATR-basierten Bandes um den EMA bewegt. Wenn sich der Markt gegen die Position entwickelt, fügt die Strategie mit schrittweise skalierten prozentualen Abweichungen (DCA) hinzu. Gewinn wird genommen, wenn der Preis zum gemittelten Einstieg plus einem festen Prozentsatz zurückkehrt.

## Parameter
- Candle Type – zu verarbeitende Kerzentypen.
- EMA Length – Periode für den EMA-Trendfilter.
- ATR Length – Periode für ATR.
- ATR Mult – Multiplikator für ATR-Bänder.
- TP % – Take-Profit-Prozentsatz vom durchschnittlichen Einstieg.
- Base Deviation % – anfängliche Abweichung für das erste DCA-Niveau.
- Step Scale – Multiplikator für die Abweichung bei jedem neuen DCA-Niveau.
- DCA Size Multiplier – Volumen-Multiplikator für jede DCA-Order.
- Max DCA Levels – maximale Anzahl von Mittelungseinstiegen.
- Initial Volume – Volumen der ersten Order.
