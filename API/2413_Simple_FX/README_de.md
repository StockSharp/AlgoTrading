# Einfache FX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie verwendet zwei exponentielle gleitende Durchschnitte zur Erkennung von Trendwechseln. Eine Long-Position wird eröffnet, wenn der kurze EMA den langen EMA von unten kreuzt, während eine Short-Position eröffnet wird, wenn der kurze EMA den langen EMA von oben kreuzt.

## Parameter
- **Long MA Period** – Periode des langen EMA.
- **Short MA Period** – Periode des kurzen EMA.
- **Stop Loss (points)** – Schutz-Stop in Preisschritten.
- **Take Profit (points)** – Gewinnziel in Preisschritten.
- **Candle Type** – Zeitrahmen für Kerzen.
