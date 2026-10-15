# Fortgeschrittene EMA-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht long, wenn ein kurzfristiger EMA einen langfristigen EMA nach oben kreuzt, während Signale mit normalisiertem ATR, ADX-Trendstärke und einer SuperTrend-Richtungsprüfung gefiltert werden. Stop-Loss- und Take-Profit-Niveaus passen sich basierend auf der USD-Stärke an, die aus einem 50-Perioden-EMA abgeleitet wird.

## Details

- **Einstiegskriterien**:
  - Kurzer EMA kreuzt über langen EMA.
  - Normalisierter ATR über Schwellenwerten je nach Trendrichtung.
  - SuperTrend bestätigt Bullen- oder Bärenmarkt.
- **Ausstiegskriterien**:
  - Gegenteilige EMA-Kreuzung oder ADX über Schwellenwert nach einer Mindesthaltedauer.
  - Stop-Loss oder Take-Profit erreicht.
- **Indikatoren**: EMA, ATR, ADX, SuperTrend, SMA (Volumen).
- **Stops**: Dynamischer prozentualer Stop-Loss und Take-Profit.
- **Typ**: Trendfolge.
- **Zeitrahmen**: 30 Minuten (Standard).
