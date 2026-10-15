# Doppel-AI-SuperTrend-Handelsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet zwei SuperTrend-Indikatoren in Kombination mit gewichteten gleitenden Durchschnitten, um die Trendrichtung zu bestätigen. Long-Trades werden eröffnet, wenn beide SuperTrends bullisch sind und die Preis-WMAs oberhalb der entsprechenden SuperTrend-WMAs bleiben. Short-Trades entstehen bei den entgegengesetzten Bedingungen. Positionen werden mit einem ATR-basierten Trailing-Stop des ersten SuperTrends verwaltet.

- **Long**: Beide SuperTrends bullisch und Preis-WMAs über SuperTrend-WMAs.
- **Short**: Beide SuperTrends bärisch und Preis-WMAs unter SuperTrend-WMAs.
- **Indikatoren**: SuperTrend, WMA, ATR.
- **Stops**: Trailing-Stop basierend auf dem ersten SuperTrend.
