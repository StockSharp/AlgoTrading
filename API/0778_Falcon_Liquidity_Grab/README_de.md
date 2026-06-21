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
