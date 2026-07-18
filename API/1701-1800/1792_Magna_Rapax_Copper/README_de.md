# Magna Rapax Copper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das „Regenbogen"-System gleitender Durchschnitte aus dem ursprünglichen MQL-Experten.
Es werden elf exponentielle gleitende Durchschnitte zusammen mit MACD- und ADX-Filtern verwendet.

## Funktionsweise

- Berechnung von EMA(2), EMA(3), EMA(5), EMA(8), EMA(13), EMA(21), EMA(34), EMA(55), EMA(89), EMA(144) und EMA(233) auf Schlusskursen.
- Berechnung von MACD (Schnell, Langsam, Signal) und Verwendung der Signallinie.
- Berechnung von ADX zur Messung der Trendstärke.
- **Kaufen**, wenn:
  - Die MACD-Signallinie über null liegt.
  - Alle EMAs strikt aufsteigend sind (jede schnellere EMA über der langsameren).
  - Der ADX-Wert über dem Schwellenwert liegt.
- **Verkaufen**, wenn:
  - Die MACD-Signallinie unter null liegt.
  - Alle EMAs strikt absteigend sind.
  - Der ADX-Wert über dem Schwellenwert liegt.

Positionen werden umgekehrt, wenn ein entgegengesetztes Signal erscheint.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `FastMacd` | Schnelle EMA-Periode für MACD. |
| `SlowMacd` | Langsame EMA-Periode für MACD. |
| `SignalPeriod` | Signallinien-Periode für MACD. |
| `AdxPeriod` | Periode für den ADX-Indikator. |
| `AdxThreshold` | Mindest-ADX-Wert zum Handeln. |
| `CandleType` | Kerzen-Zeitrahmen für Berechnungen. |

## Hinweise

- Die Strategie verwendet Marktorders über `BuyMarket` und `SellMarket`.
- Es wird jeweils nur eine Position gehalten; ein entgegengesetztes Signal kehrt die Position um.
- Dies ist eine direkte Konvertierung der ursprünglichen MQL-Strategie ohne die optionale Martingal-Logik.
