# Volumen-pro-Punkt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet das Volumen pro Preispunkt für jede Kerze. Ein Long-Trade wird eröffnet, wenn die Kerzenrange sinkt, das Volumen aber steigt und der RSI-Filter (falls aktiviert) das Signal bestätigt. Ein Short-Trade wird eröffnet, wenn die Range sich ausweitet, während das Volumen schrumpft.

## Parameter
- **RSI Length** – Periode für die RSI-Berechnung.
- **RSI Above/Below** – Schwellenwerte für den optionalen RSI-Filter.
- **Use RSI Filter** – RSI-Filterung aktivieren oder deaktivieren.
- **Candle Type** – Zeitrahmen der Eingabekerzen.
