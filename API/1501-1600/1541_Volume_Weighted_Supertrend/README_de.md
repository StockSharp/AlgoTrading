# Volumengewichtete Supertrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet einen Supertrend auf Basis eines volumengewichteten gleitenden Durchschnitts und eines ATR-Bandes. Ein zweiter Supertrend wird auf das Volumen angewendet, um die Trendstärke zu bestätigen. Eine Long-Position wird eröffnet, wenn Volumen- und Preistrend gemeinsam nach oben zeigen, und geschlossen, wenn sich die Bedingungen umkehren.

## Parameter
- **ATR Period** – ATR-Zeitraum für den Preistrend.
- **Volume Period** – Zeitraum für VWAP und Volumentrend.
- **Factor** – ATR-Multiplikator.
- **Candle Type** – Zeitrahmen der verarbeiteten Kerzen.
