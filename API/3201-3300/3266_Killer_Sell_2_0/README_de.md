# Strategie Killer Sell 2.0 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Killer Sell 2.0 ist ein reiner Short-MetaTrader-4-Expertenberater, der Einstiege nach ausgedehnten überkauften Readings timed und Gewinne sichert, wenn der Momentum in überverkauftes Gebiet schwenkt. Dieser Port schreibt die ursprüngliche Logik auf Basis der StockSharp High-Level-Strategie-API um. Die gesamte Indikatorverarbeitung ist ereignisgesteuert über `SubscribeCandles().BindEx(...)`, und Money-Management-Regeln sind innerhalb der Strategieklasse gekapselt.

## Handelslogik
Die konvertierte Logik folgt der ursprünglichen Signalkette und verwendet das Nettopositionsmodell von StockSharp. Jede abgeschlossene Kerze des konfigurierten Zeitrahmens führt folgende Schritte aus:

1. **Datenvorbereitung.** Die Strategie aktualisiert einen MACD (12/120/9), Williams %R (Periode 350 für beide Filter) und zwei Stochastik-Oszillatoren (10/1/3 für Einstieg, 90/7/1 für Ausstiege). Indikatorwerte werden nur konsumiert, wenn der neue Balken fertig und die Eingaben vollständig gebildet sind.
2. **Einstiegsfilter.** Ein Short-Setup ist gültig, wenn alle folgenden Bedingungen erfüllt sind:
   - Williams %R steigt über −10, was einen überkauften Markt signalisiert.
   - Die MACD-Hauptlinie ist größer als `0.0014`.
   - Der Einstiegs-Stochastik %K kreuzt **unter** das konfigurierbare Einstiegsniveau (Standard 90). Die Kreuzungserkennung erfolgt auf aufeinanderfolgenden %K-Lesungen.
3. **Orderplatzierung.** Sobald die Filter übereinstimmen, sendet die Strategie einen Marktverkauf mit der aktuellen Martingale-Lot-Größe. Orders erhalten einen Take-Profit `N` Pips entfernt (Standard 100 Pips) über `StartProtection`.
4. **Ausstiegsverwaltung.** Während ein Short-Exposure besteht, berechnet die Strategie das arithmetische Mittel des Gewinns offener Tickets in Pips. Abhängig vom Momentum:
   - Wenn der durchschnittliche Gewinn **unter** 10 Pips liegt und Williams %R unter −80 fällt, werden alle Shorts sofort geschlossen.
   - Wenn der durchschnittliche Gewinn **über** 15 Pips liegt und der Exit-Stochastik %K unter 12 fällt, wird die Position zur Gewinnmitnahme geschlossen.

## Money Management
Killer Sell 2.0 verwendet eine Martingale-Leiter ähnlich dem originalen EA. Die StockSharp-Implementierung führt eine interne Liste offener Short-Lots, um die Per-Ticket-Berechnungen von MetaTrader nachzuahmen:

- Der erste Trade verwendet `InitialVolume` (Standard 0.05 Lots).
- Nach einem profitablen oder Breakeven-Zyklus wird das Volumen auf die anfängliche Lot-Größe zurückgesetzt.
- Nach einem Verlustzyklus wird der nächste Auftrag mit `MartingaleMultiplier` (Standard ×1.2) multipliziert. Ein Sicherheitsdeckel `MaxVolume` verhindert unkontrolliertes Wachstum.

Der Helfer verfolgt auch realisiertes PnL bei Fills, um zu entscheiden, ob der vorherige Zyklus profitabel war.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Primärer Zeitrahmen, der jeden Indikator versorgt. |
| `EntryWprPeriod` / `ExitWprPeriod` | Williams %R-Längen für Einstiegs- und Ausstiegsbestätigungen. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD-Konfiguration. |
| `MacdThreshold` | Mindestwert der MACD-Hauptlinie für einen Verkauf. |
| `StochasticEntryKPeriod`, `StochasticEntryDPeriod`, `StochasticEntrySlow` | Einstiegs-Stochastik-Parameter. |
| `EntryStochasticLevel` | Niveau, das %K von oben kreuzen muss, um ein Signal zu validieren. |
| `StochasticExitKPeriod`, `StochasticExitDPeriod`, `StochasticExitSlow` | Ausstiegs-Stochastik-Parameter. |
| `ExitStochasticLevel` | Überverkaufte Grenze, die vor Gewinnmitnahme geprüft wird. |
| `EntryWprThreshold` / `ExitWprThreshold` | Williams %R-Schwellenwerte für Einstiege/Ausstiege. |
| `LossExitPips` / `ProfitExitPips` | Durchschnittliche Gewinngrenzen (in Pips) für defensive und Zielausstiege. |
| `TakeProfitPips` | Schützender Take-Profit für jede Verkaufsorder. |
| `InitialVolume` | Volumen des ersten Martingale-Schritts. |
| `MartingaleMultiplier` | Faktor nach Verlusten. |
| `MaxVolume` | Absolutes Cap für die nächste Lot-Größe. |

## Konvertierungshinweise
- MetaTrader führt einzelne Tickets; StockSharp arbeitet mit einer Nettoposition. Die Strategie speichert daher jeden gefüllten Short (Volumen + Preis), um durchschnittliche Gewinnberechnungen zu reproduzieren und Martingale-Resets zu evaluieren.
- Der MT4-"Martingale"-Block exponierte viele zusätzliche Modi (fest, prozentuales Risiko, 1326, Fibonacci usw.). Die ursprüngliche Konfiguration verwendete den einfachen Martingale-Zweig; nur dieses Verhalten wird hier repliziert.
- Der Notfall-Stop-Loss war im Quellprojekt deaktiviert. Der Port spiegelt diese Einrichtung, indem nur ein Take-Profit angehängt und andere Ausstiege intern behandelt werden.

## Verwendungstipps
1. Fügen Sie die Strategie einem Portfolio und Wertpapier hinzu, dann setzen Sie denselben Zeitrahmen wie in den MT4-Backtests (die Standardwerte gehen von H1 aus).
2. Stellen Sie sicher, dass Marktdaten abgeschlossene Kerzen liefern; Indikatoren verlassen sich auf `CandleStates.Finished`-Ereignisse.
3. Überprüfen Sie die Kontohebel und erlaubten Lot-Größen. Der Standard-Martingale-Deckel (5 Lots) sollte an Ihre Broker-Anforderungen angepasst werden.
4. Testen Sie gründlich — Martingale-Strategien verstärken das Risiko, wenn Märkte stark gegen den Short-Bias tendieren.
