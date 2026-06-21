# Genie Stoch RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt mithilfe einer Kombination aus dem Relative Strength Index (RSI) und dem Stochastic Oscillator.
Sie wartet darauf, dass der Markt überverkaufte oder überkaufte Zonen erreicht, und sucht dann nach einem Kreuzung zwischen der
Stochastic-Hauptlinie und ihrer Signallinie, um die Umkehr zu bestätigen. Zur Risikoverwaltung werden ein Trailing-Stop
und ein fester Take-Profit angewendet.

## Logik

1. Kerzen des ausgewählten Zeitrahmens abonnieren.
2. RSI mit konfigurierbarer Periode berechnen.
3. Stochastic Oscillator mit konfigurierbaren %K-, %D- und Verlangsamungsperioden berechnen.
4. Für einen Long-Einstieg:
   - RSI liegt unter dem überverkauften Niveau.
   - %K liegt unter dem überverkauften Niveau des Stochastic.
   - Vorheriges %K liegt unter vorherigem %D und aktuelles %K kreuzt aktuelles %D nach oben.
5. Für einen Short-Einstieg:
   - RSI liegt über dem überkauften Niveau.
   - %K liegt über dem überkauften Niveau des Stochastic.
   - Vorheriges %K liegt über vorherigem %D und aktuelles %K kreuzt aktuelles %D nach unten.
6. Die Positionsgröße wird aus der Eigenschaft `Volume` der Strategie entnommen. Bestehende Positionen werden umgekehrt, wenn ein
   entgegengesetztes Signal erscheint.
7. `StartProtection` aktiviert einen Trailing-Stop und Take-Profit, gemessen in Preispunkten.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `RsiPeriod` | RSI-Berechnungslänge. |
| `KPeriod` | Stochastic %K-Periode. |
| `DPeriod` | Stochastic %D-Periode. |
| `Slowing` | Stochastic-Verlangsamungswert. |
| `RsiOverbought` | RSI-Niveau, das als überkauft gilt. |
| `RsiOversold` | RSI-Niveau, das als überverkauft gilt. |
| `StochOverbought` | Stochastic-Niveau, das als überkauft gilt. |
| `StochOversold` | Stochastic-Niveau, das als überverkauft gilt. |
| `TakeProfit` | Take-Profit-Distanz in Preispunkten. |
| `TrailingStop` | Trailing-Stop-Distanz in Preispunkten. |
| `CandleType` | Kerzentyp und Zeitrahmen für die Analyse. |

## Hinweise

Die Strategie verarbeitet nur abgeschlossene Kerzen und ignoriert Signale, bis alle Indikatoren vollständig ausgebildet sind.
Sie ist als Lehrbeispiel gedacht und sollte vor dem Live-Trading gründlich getestet werden.
