# MultiLayer Awesome Oscillator Saucer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert eine bullische Multi-Layer-Strategie auf Basis des Saucer-Musters des Awesome Oscillators und der fraktalbasierten Trenderkennung. Die Strategie zählt aufeinanderfolgende Saucer-Signale und platziert bis zu fünf gestaffelte Buy-Stop-Orders oberhalb des Kurses. Positionen werden geschlossen, wenn der Trend dreht.

## Parameter
- **EMA Length** – Periode des EMA-Filters.
- **Candle Type** – Kerzentyp.
- **Trade Start** – Beginn der Handelsperiode.
- **Trade Stop** – Ende der Handelsperiode.
