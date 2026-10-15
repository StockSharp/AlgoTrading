# Fibonacci-Time-Zones-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine StockSharp-Portierung des MetaTrader Expert Advisors "Fibonacci Time Zones". Sie bewahrt den diskretionären Charakter des ursprünglichen Skripts, indem sie einen MACD-Filter auf höherem Zeitrahmen mit Bollinger-Band-Ausstiegen und einem umfangreichen Money-Management-Modul kombiniert. Alle Trade-Management-Routinen wurden mit der High-Level-API neu geschrieben: Die Strategie abonniert zwei Kerzenströme (einen Handelszeitrahmen und einen langsameren Zeitrahmen für die MACD-Bestätigung) und bindet Indikatoren direkt über `Bind`/`BindEx`-Callbacks.

## Kernlogik

1. **Momentum-Filter** - Ein monatliches (konfigurierbares) MACD-Histogramm wird berechnet. Eine bullische Kreuzung über die Signallinie plant Long-Einstiege, während eine bärische Kreuzung Short-Einstiege plant. Die tatsächliche Position wird auf der nächsten Handelskerze eröffnet, um wiederholte Orders auf derselben Kreuzung zu vermeiden.
2. **Einstiegsausführung** - Jedes Signal sendet eine benutzerdefinierte Anzahl von Marktorders. Bestehende Gegenexposure wird vor dem Eröffnen einer neuen Position glattgestellt.
3. **Ausstiegsregeln** - Mehrere Verteidigungsebenen werden angewendet:
   - **Bollinger-Band-Ausstieg**: Longs werden geschlossen, wenn der Preis das obere Band berührt; Shorts, wenn das untere Band erreicht wird.
   - **Klassischer Stop/Ziel**: Statische Stop-Loss-, Take-Profit- und Trailing-Stop-Distanzen werden von Pips in Preiseinheiten umgerechnet und an `StartProtection` übergeben.
   - **Break-even**: Nachdem der Preis eine konfigurierbare Anzahl von Pips zurückgelegt hat, wird der Stop auf Break-even plus Offset gezogen. Fällt der Preis auf dieses Niveau zurück, wird die Position geschlossen.
   - **Money-Trailing**: Offener und realisierter PnL werden überwacht. Wenn der schwebende Gewinn einen Schwellenwert erreicht, beginnt die Strategie ihn zu trailen und schließt nach einem konfigurierbaren Drawdown alles.
   - **Equity-Ziele**: Optionale absolute oder prozentuale Gewinnziele schließen alle Trades sofort, wenn sie erreicht werden.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `UseTakeProfitMoney`, `TakeProfitMoney` | Schließt alle Positionen, wenn der kombinierte Gewinn (realisiert + unrealisiert) den angegebenen Betrag in Kontowährung erreicht. |
| `UseTakeProfitPercent`, `TakeProfitPercent` | Ähnlich der vorherigen Option, jedoch als Prozentsatz des Start-Equity gemessen. |
| `EnableTrailingProfit`, `TrailingTakeProfitMoney`, `TrailingStopLossMoney` | Aktiviert geldbasiertes Trailing, sobald der erste Schwellenwert erreicht ist, und schützt angesammelte Gewinne. |
| `UseStop`, `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Klassischer Stop, Ziel und Trailing-Distanzen in Pips. |
| `UseMoveToBreakEven`, `WhenToMoveToBreakEven`, `PipsToMoveStopLoss` | Steuert das Break-even-Verhalten. |
| `NumberOfTrades` | Anzahl der für jedes Signal gesendeten Marktorders (imitiert den ursprünglichen EA, der Einstiege stapeln konnte). |
| `CandleType`, `MacdCandleType` | Zeitrahmen für die Management-Kerzen und den MACD-Filter. |

## Unterschiede zum ursprünglichen EA

* Chart-Schaltflächen und grafische Fibonacci-Objekte werden nicht nachgebildet; die StockSharp-Portierung konzentriert sich ausschließlich auf systematische Ausführung.
* Der ursprüngliche Expert Advisor handelte über manuelle Schaltflächenklicks. Die Portierung steigt automatisch bei MACD-Kreuzungen ein, um eine deterministische und backtestbare Strategie zu liefern.
* MetaTrader-spezifische Kontofunktionen wurden durch StockSharp-Entsprechungen ersetzt (`Portfolio`-Werte und `PnL`).

## Nutzungstipps

1. Wählen Sie geeignete Kerzentypen, bevor Sie die Strategie starten. Die Standardwerte entsprechen einem 15-Minuten-Handelschart mit monatlichem MACD-Filter.
2. Stimmen Sie die pipbasierten Distanzen auf die Tickgröße des Instruments ab. Die Strategie rechnet Pips intern über `Security.PriceStep` in Preise um.
3. Für diskretionäres Eingreifen deaktivieren Sie die automatischen Gewinnziele und verwenden nur den Bollinger-Ausstieg.
