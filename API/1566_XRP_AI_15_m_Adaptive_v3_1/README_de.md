# Adaptive XRP AI 15-m-Strategie v3.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt XRP auf 15-Minuten-Kerzen mit einem Trendfilter auf höherem Zeitrahmen. Sie wählt zwischen kleinen Rücksetzern, mittleren Volumen-Flushes oder großen Momentum-Ausbrüchen und wendet ATR-basierte Stops, Ziele, Trailing-Stop und einen zeitbasierten Ausstieg an.

## Parameter
- **Risk Mult** – ATR-Multiplikator für den Anfangs-Stop.
- **Small TP** – ATR-Multiplikator für Take-Profit bei einem kleinen Rücksetzer.
- **Med TP** – ATR-Multiplikator für Take-Profit bei einem mittleren Volumen-Flush.
- **Large TP** – ATR-Multiplikator für Take-Profit bei einem großen Momentum-Ausbruch.
- **Volume Mult** – SMA-20-Volumen-Multiplikator zur Erkennung von Spitzen.
- **Trail Percent** – Trailing-Stop-Prozent des ATR vom höchsten Preis.
- **Trail Arm** – offener Gewinn in ATR-Vielfachen bevor der Trailing aktiviert wird.
- **Max Bars** – maximale Anzahl von 15-Minuten-Kerzen für eine Position.
- **Candle Type** – Kerzentyp für Hauptberechnungen.
- **Trend Candle Type** – Kerzentyp für den Trendfilter.
