# MACD der Relativen Stärke Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet die relative Stärke durch Division des Schlusskurses durch das höchste Hoch über einen bestimmten Zeitraum und wendet den MACD-Indikator auf dieses Verhältnis an. Eine Long-Position wird eröffnet, wenn das MACD-Histogramm positiv ist, und geschlossen, wenn es negativ wird. Ein prozentualer Stop-Loss schützt den Handel.

## Details
- **Einstieg**: Histogramm > 0.
- **Ausstieg**: Histogramm < 0 oder Stop-Loss.
- **Typ**: Nur Long.
- **Indikatoren**: Highest, MACD.
