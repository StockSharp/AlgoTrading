# Udy Ivan Madumere Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der Fachberater von Udy Ivan Madumere eröffnet einmal täglich eine einzelne Marktposition, wenn eine bestimmte stündliche Kerze erscheint. Der Port StockSharp hält dieses Verhalten aufrecht, indem er die konfigurierte Kerzenserie beobachtet, historische Eröffnungspreise vergleicht und unmittelbar nach dem Schließen des Zielbalkens reagiert. Alle Ausführungsentscheidungen, die Positionsverwaltung und die Volumenverwaltung werden reproduziert, sodass sich die Strategie in der StockSharp-Umgebung wie das Original von MetaTrader 4 verhält.

Hauptmerkmale:

- Bewertet eine fertige Kerze pro Tag bei `TradeHour` und übermittelt nie mehr als eine gleichzeitige Position.
- Misst die Differenz zwischen den Eröffnungspreisen `Open[FirstLookback]` und `Open[SecondLookback]`, um zu entscheiden, ob Short- oder Long-Positionen eingegangen werden sollen.
- Spiegelt die Saldoleiter von MetaTrader wider, um die Basislosgröße automatisch anzupassen, wenn `UseAutoVolume` aktiviert ist.
- Wendet asymmetrische Stop-Loss- und Take-Profit-Abstände (getrennt für Long und Short) sowie einen Trailing Stop an, der nur Short-Positionen betrifft.
- Erzwingt die Schließung jedes Handels nach einer konfigurierbaren Anzahl von Stunden, auch wenn die Schutzniveaus nicht erreicht wurden.

## Handelsablauf
1. Abonnieren Sie den ausgewählten Kerzentyp (`CandleType`) und warten Sie, bis die Balken vollständig fertiggestellt sind, um vorzeitige Signale zu verhindern.
2. Verfolgen Sie den Verlauf der offenen Preise, damit die Unterschiede `Open[FirstLookback] - Open[SecondLookback]` (kurze Einrichtung) und `Open[SecondLookback] - Open[FirstLookback]` (lange Einrichtung) genau wie in MetaTrader ausgewertet werden können.
3. Wenn die letzte Kerze um `TradeHour` öffnet:
   - Wenn die rückläufige Differenz größer als `ShortDeltaPoints * PriceStep` ist, senden Sie einen Marktverkaufsauftrag.
   - Andernfalls, wenn die bullische Differenz `LongDeltaPoints * PriceStep` überschreitet, senden Sie eine Marktkauforder.
4. Es ist nur eine Bestellung pro Tag zulässig. Das Flag `canTrade` wird nach Ablauf der konfigurierten Stunde zurückgesetzt, um einen weiteren Versuch in der nächsten Sitzung zu ermöglichen.
5. Bei der Auftragseingabe berechnet die Strategie das Basislos neu:
   - `UseAutoVolume = true` aktiviert die Legacy-Leiter, die die Losgröße erhöht, wenn der Kontostand vordefinierte Schwellenwerte überschreitet.
   - Wenn der aktuelle Saldo unter dem Snapshot des vorherigen Handels liegt, wird das Ergebnis mit `BigLotMultiplier` multipliziert, was dem „Big Lot“-Erholungsverhalten des EA entspricht.
6. Während die Position offen ist, wird bei jeder abgeschlossenen Kerze die folgende Exit-Logik ausgeführt:
   - Harter Take-Profit und Stop-Loss werden anhand des erfassten Einstiegspreises bewertet.
   - Auch Short-Trades folgen dem Stop, sobald sich der beste Preis um mindestens `TrailingStopPoints` verbessert hat.
   - Die Position wird zwangsweise geschlossen, sobald sie `MaxHoldingHours` aktiv war.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H1` | Von der Strategie verarbeitete Kerzenserie. |
| `TradeHour` | `int` | `18` | Stunde des Tages (0-23), zu der das Tagessignal ausgewertet wird. |
| `FirstLookback` | `int` | `6` | Anzahl der abgeschlossenen Kerzen, referenziert als `Open[FirstLookback]`. |
| `SecondLookback` | `int` | `2` | Anzahl der abgeschlossenen Kerzen, referenziert als `Open[SecondLookback]`. |
| `LongDeltaPoints` | `decimal` | `6` | Minimale bullische Eröffnungspreisdifferenz (in MetaTrader Punkten), die für den Einstieg in eine Long-Position erforderlich ist. |
| `ShortDeltaPoints` | `decimal` | `21` | Minimale bärische Eröffnungspreisdifferenz (in MetaTrader Punkten), die für den Einstieg in einen Short erforderlich ist. |
| `TakeProfitLongPoints` | `decimal` | `39` | Take-Profit-Distanz, ausgedrückt in Punkten, für Long-Positionen. |
| `StopLossLongPoints` | `decimal` | `147` | Stop-Loss-Distanz in Punkten für Long-Positionen. |
| `TakeProfitShortPoints` | `decimal` | `200` | Take-Profit-Distanz in Punkten für Short-Positionen. |
| `StopLossShortPoints` | `decimal` | `267` | Stop-Loss-Distanz in Punkten für Short-Positionen. |
| `TrailingStopPoints` | `decimal` | `30` | Trailing-Stop-Distanz (Punkte), gilt nur für Short-Positionen. |
| `BaseVolume` | `decimal` | `0.01` | Anfängliche Losgröße vor Anpassungen des Geldmanagements. |
| `UseAutoVolume` | `bool` | `true` | Aktivieren Sie die MetaTrader-Guthabenleiter, die `BaseVolume` überschreibt. |
| `BigLotMultiplier` | `decimal` | `1` | Ein zusätzlicher Multiplikator wird angewendet, wenn der Kontostand seit dem vorherigen Handel gesunken ist. |
| `MaxHoldingHours` | `int` | `504` | Maximale Haltezeit in Stunden. Null deaktiviert den Timer. |

## Hinweise zur Implementierung
- Preisschwellenwerte werden mithilfe der `PriceStep` des Instruments von MetaTrader „Punkten“ in tatsächliche Preisabstände umgewandelt.
- Der offene Preispuffer wird auf `max(FirstLookback, SecondLookback) + 1` Einträge reduziert, wodurch unnötige Zuteilungen vermieden werden und gleichzeitig die erforderliche Historie erhalten bleibt.
- Der Trailing-Stop für Short-Trades speichert den besten erreichten Tiefststand und aktualisiert das Schutzniveau nur, wenn der neue Kandidat näher am aktuellen Preis liegt.
- Kontostand-Snapshots basieren auf `Portfolio.CurrentValue` (Rückfall auf `BeginValue`), sodass sich Demo-, Live- und Backtest-Umgebungen konsistent verhalten.
- Jeder Kommentar im Code ist wie gewünscht in Englisch verfasst, sodass die Logik leicht überprüft oder erweitert werden kann.

## Anwendungstipps
- Ordnen Sie `CandleType` dem Zeitrahmen zu, der vom historischen EA verwendet wird (die ursprüngliche Vorlage erwartet einstündige Kerzen).
- Wenn Sie auf Symbolen laufen, die Mikrolots verwenden, passen Sie `BaseVolume` und die Werte der Auto-Lot-Leiter an die Vertragsspezifikationen des Veranstaltungsortes an.
- Kombinieren Sie die Strategie mit StockSharp-Diagrammen über die integrierten Helfer (`DrawCandles`, `DrawOwnTrades`), um sicherzustellen, dass Bestellungen nur einmal pro Tag zur konfigurierten Stunde erscheinen.
