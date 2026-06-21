# Intraday Beta-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sucht nach Intraday-Wendepunkten anhand der Steigungen geglätteter gleitender Durchschnitte und des Relative Strength Index (RSI).
Eine Long-Position wird eröffnet, wenn die Steigung des 10-Perioden-gleitenden Durchschnitts nach einer Abwärtsbewegung aufwärts dreht, der RSI unter 70 liegt
und die vorherige Kerze bullisch ist. Eine Short-Position wird eröffnet, wenn die Steigung nach einer Aufwärtsbewegung abwärts dreht, der RSI
über 30 liegt und die vorherige Kerze bärisch ist.

Ein Average True Range (ATR)-Filter blockiert neue Einstiege, wenn die Volatilität zu hoch ist. Offene Positionen werden durch einen adaptiven
Trailing-Stop geschützt, der sich zugunsten des Handels bewegt und aussteigt, wenn der Preis das Stop-Niveau kreuzt.

## Parameter
- **RSI Period** – Periode des RSI-Indikators.
- **Trailing Stop** – Trailing-Stop-Abstand in Preiseinheiten.
- **ATR Threshold** – maximaler ATR-Wert für den Handel.
- **Candle Type** – Zeitrahmen der für die Analyse verwendeten Kerzen.
