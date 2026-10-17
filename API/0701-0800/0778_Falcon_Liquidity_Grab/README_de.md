# Falcon Liquidity Grab-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Liquiditätsgrabbing während der wichtigsten Marktsitzungen und verwendet einen einfachen gleitenden Durchschnitt zur Trendbestimmung. Es wird eingestiegen, wenn der Preis jüngste Swing-Levels durchbricht und mit dem Trend umkehrt. Jeder Trade verwendet festen Stop-Loss und Take-Profit in Ticks.

## Details

- **Einstiegsbedingungen**:
  - **Long**: `Low < lowest(swing period)` && `Close > SMA` && `session filter`
  - **Short**: `High > highest(swing period)` && `Close < SMA` && `session filter`
- **Ausstiegsbedingungen**: fester Stop-Loss und Take-Profit.
- **Typ**: Umkehr
- **Indikatoren**: SMA, Highest, Lowest
- **Zeitrahmen**: 15 Minuten (Standard)
- **Stops**: `StopLossPoints` Ticks, `TakeProfitMultiplier`× Stopabstand

**Exakte Regel und Standardwerte:** `SwingPeriod=20`, `TrendLength=50`, `StopLossPoints=50`, `TakeProfitMultiplier=2`, `UseSession=true`, `SessionStart=07:00`, `SessionEnd=21:00` UTC, `CandleType=15 Minuten`. Docht-Tiefs/-Hochs werden streng mit den Extrema der **vorherigen** N abgeschlossenen Kerzen verglichen (ohne Signalkerze). Long bei Low unter dem bisherigen Minimum und Close über SMA; Short bei High über dem bisherigen Maximum und Close unter SMA. SMA enthält die Signalkerze. Einstiege nur ohne Position innerhalb des UTC-Fensters (Start inklusiv, Ende exklusiv; auch über Mitternacht). Native Marktorder-Absicherung auf Level1-Bid/Ask zwischen Kerzen: Stop `StopLossPoints × Security.PriceStep`, Ziel `TakeProfitMultiplier × Stop-Abstand`. Kein Exit durch Gegenanzeige beim Kerzenschluss.
