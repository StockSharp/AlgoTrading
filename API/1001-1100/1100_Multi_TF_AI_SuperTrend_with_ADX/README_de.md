# Multi-TF AI SuperTrend mit ADX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert zwei SuperTrend-Indikatoren, die durch eine ADX-Stärkeprüfung gefiltert werden. Die Trendrichtung wird durch den Vergleich der Preis-WMAs mit den SuperTrend-WMAs bestätigt. Long-Trades werden eröffnet, wenn beide SuperTrends bullisch sind und der ADX positive Stärke zeigt. Short-Trades werden unter entgegengesetzten Bedingungen eröffnet. Der ATR des ersten SuperTrend liefert einen Trailing-Stop.

- **Long**: Beide SuperTrends bullisch, Preis-WMAs über SuperTrend-WMAs, +DI > -DI und ADX über dem Schwellenwert.
- **Short**: Beide SuperTrends bärisch, Preis-WMAs unter SuperTrend-WMAs, -DI > +DI und ADX über dem Schwellenwert.
- **Indikatoren**: SuperTrend, WMA, ATR, ADX.
- **Stops**: ATR-basierter Trailing-Stop vom ersten SuperTrend.
