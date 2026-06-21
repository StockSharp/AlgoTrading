# Einfache aggressive MACD-Scalping-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert eine Scalping-Strategie mit dem MACD-Histogramm und einem 50-Perioden-EMA-Filter.

- Geht Long, wenn das MACD-Histogramm über null kreuzt und der Kurs über der EMA liegt.
- Geht Short, wenn das Histogramm unter null kreuzt und der Kurs unter der EMA liegt.
- Schließt Positionen, wenn der Momentum des Histogramms umkehrt.
