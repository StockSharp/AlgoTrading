# Volumen-ValueWhen-Geschwindigkeit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sucht nach Long-Einstiegen, wenn das Volumen zunimmt, der Markt auf Basis des RSI überverkauft ist, die durch ATR gemessene Volatilität abnimmt und der Abstand zwischen den letzten SMA-Ausbrüchen einen bestimmten Wert überschreitet. Wenn alle Bedingungen erfüllt sind, wird eine Markt-Kauforder ausgelöst.

## Parameter
- **RSI Length** – Periode für den RSI.
- **RSI Oversold** – Überverkauft-Schwellenwert.
- **ATR Small / ATR Big** – Perioden für den ATR-Vergleich.
- **Distance** – Mindestdifferenz zwischen Ausbruchspreisen.
- **Candle Type** – Zeitrahmen der Eingabekerzen.
