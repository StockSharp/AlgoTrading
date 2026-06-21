# Supertrend - SSL-Strategie mit Umschalter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den Supertrend-Indikator mit dem SSL-Kanal.
Ein Umschalter ermöglicht es, vor dem Einstieg eine Bestätigung beider Indikatoren zu verlangen.
Wenn die Bestätigung aktiviert ist, wartet das erste Indikatorsignal auf das zweite, bevor es ausgeführt wird.
Positionen werden geschlossen, wenn ein entgegengesetztes Signal von einem der Indikatoren erscheint.

## Details

- **Indikatoren**: Supertrend (ATR 10, Faktor 2.4), SSL-Kanal (Periode 13)
- **Einstieg**: SSL-Crossover oder Supertrend-Richtungsänderung mit optionaler Bestätigung
- **Ausstieg**: Entgegengesetztes Signal von SSL oder Supertrend
- **Richtung**: Long und Short
- **Zeitrahmen**: Beliebig
