# ADX Smoothed Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung

Die Strategie handelt auf Basis eines doppelt geglätteten Average Directional Index (ADX). Sie vergleicht die geglätteten +DI- und -DI-Linien, um Trendwechsel zu erkennen. Wenn die geglättete +DI-Linie die geglättete -DI-Linie von unten kreuzt, eröffnet die Strategie eine Long-Position. Wenn die geglättete +DI-Linie die geglättete -DI-Linie von oben kreuzt, wird eine Short-Position eröffnet.

## Funktionsweise

- Verwendet einen ADX-Indikator mit konfigurierbarer Periode.
- Wendet zwei Durchläufe der exponentiellen Glättung an, gesteuert durch die Parameter **Alpha1** und **Alpha2**.
- Ein Long-Signal tritt auf, wenn das vorherige geglättete +DI unter dem geglätteten -DI lag und das aktuelle geglättete +DI darüber liegt.
- Ein Short-Signal tritt beim umgekehrten Kreuzungsverhalten auf.
- Optionale Parameter ermöglichen das Deaktivieren von Long- oder Short-Trades und steuern, ob bestehende Positionen bei einem entgegengesetzten Signal geschlossen werden können.
- Das integrierte Risikomanagement setzt Stop-Loss- und Take-Profit-Niveaus in Punkten.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `AdxPeriod` | Periode für die ADX-Berechnung. |
| `Alpha1` | Erster Glättungskoeffizient (0-1). |
| `Alpha2` | Zweiter Glättungskoeffizient (0-1). |
| `StopLoss` | Stop-Loss in Punkten. |
| `TakeProfit` | Take-Profit in Punkten. |
| `AllowBuy` | Long-Einstiege aktivieren. |
| `AllowSell` | Short-Einstiege aktivieren. |
| `AllowCloseBuy` | Schließen von Long-Positionen bei Verkaufssignalen erlauben. |
| `AllowCloseSell` | Schließen von Short-Positionen bei Kaufsignalen erlauben. |
| `CandleType` | Zeitrahmen für den Indikator. |

## Hinweise

- Es werden nur abgeschlossene Kerzen verarbeitet.
- Die Strategie verwendet die High-Level-API mit Indikator-Bindung.
- Stop-Loss und Take-Profit werden über `StartProtection` verwaltet.
