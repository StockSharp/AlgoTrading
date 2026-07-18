# PresentTrend Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet ATR-basierte Schwellenwerte mit RSI oder MFI zur Verfolgung der Trendrichtung. Die PresentTrend-Linie wird durch Ausdehnung oder Kontraktion basierend auf dem Oszillatorwert und ATR aufgebaut. Signale erscheinen, wenn PresentTrend seinen Wert von vor zwei Bars kreuzt und das jüngste entgegengesetzte Signal die Richtung bestätigt.

- **Long**: PresentTrend kreuzt über seinen Wert von zwei Bars zuvor und das letzte Short-Signal war jünger als der vorherige Long.
- **Short**: PresentTrend kreuzt unter seinen Wert von zwei Bars zuvor und das letzte Long-Signal war jünger als der vorherige Short.
- **Indikatoren**: ATR, RSI oder MFI.
- **Stops**: Schließt Position, wenn im einseitigen Modus ein entgegengesetztes Signal erscheint.
