# Verbesserte Range-Filter-Strategie mit ATR TP/SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen benutzerdefinierten Range-Filter mit ATR-basierten Take-Profit- und Stop-Loss-Levels.
Einstiege erfolgen, wenn der Preis den Filter bricht und alle zusätzlichen Filter erfüllt sind:

- Volumen über dem Durchschnitt
- RSI innerhalb konfigurierter Grenzen
- Trendbestätigung durch EMA-Kreuzung
- Markt befindet sich nicht in einer Seitwärtsbewegung gemäß ATR-Verhältnis

Positionen werden geschlossen, wenn das ATR-basierte Stop-Loss- oder Take-Profit-Level erreicht wird.
