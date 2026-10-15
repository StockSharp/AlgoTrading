# N Candles v6 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **N Candles v6** Strategie überwacht die zuletzt abgeschlossenen Kerzen und sucht nach Serien identischer Richtung. Wenn der Markt `N` bullische Kerzen in Folge druckt, eröffnet die Strategie eine Long-Position, während eine Reihe von `N` bärischen Kerzen einen Short-Einstieg produziert. Die Logik ist vom MetaTrader-Expertenberater *N Candles v6.mq5* inspiriert und an die StockSharp High-Level-API angepasst.

Der Algorithmus ist für jedes Symbol konzipiert, das Standard-zeitbasierte Kerzen liefert. Ein konfigurierbares Handelsfenster hält die Strategie außerhalb der gewünschten Session inaktiv, aber die aktive Trailing- und Exit-Logik schützt weiterhin eine offene Position auch während der gesperrten Stunden.

## Handelslogik
1. Den konfigurierten Kerzentyp abonnieren und nur fertige Kerzen verarbeiten.
2. Aufeinanderfolgende bullische (`Close > Open`) und bärische (`Close < Open`) Kerzen zählen. Dojis setzen die Zähler zurück.
3. Wenn `CandlesCount` bullische Kerzen erscheinen:
   - Überprüfen, dass die projizierte Netto-Position unter `MaxPositionVolume` bleibt.
   - Eine Markt-Kauforder senden. Wenn eine Short-Position besteht, wird die Ordergröße erhöht, um die Position in einem Trade auf Long zu drehen.
4. Wenn `CandlesCount` bärische Kerzen erscheinen:
   - Sicherstellen, dass die neue Short-Exposition `MaxPositionVolume` nicht überschreitet.
   - Eine Markt-Verkaufsorder senden und die Order vergrößern, wenn eine Long-Position geschlossen werden muss.
5. Wenn die neueste Kerze die Serie bricht (das "schwarze Schaf"):
   - Den ausgewählten `ClosingMode` anwenden, um alle, entgegengesetzte oder gleichgerichtete Positionen einmal zu schließen.
6. Trailing- und Schutzausstiege laufen auf jeder Kerze:
   - Stop-Loss- und Take-Profit-Niveaus werden aus Pip-Distanzen und dem Instrument-Preisschritt abgeleitet.
   - Der Trailing Stop aktiviert sich, nachdem der Preis um `TrailingStopPips + TrailingStepPips` bewegt hat und sperrt nur in der günstigen Richtung.
   - Jede Verletzung des Stops, Take-Profits oder Trailing-Niveaus schließt sofort die gesamte Position.

## Risikomanagement
- **Stop Loss (Pips)** – konvertiert Pip-Distanz in einen absoluten Preis-Offset unter Verwendung des Symbol-Preisschritts (5- und 3-Ziffern-Instrumente werden automatisch skaliert).
- **Take Profit (Pips)** – schließt die Position nach einer günstigen Bewegung der angegebenen Größe.
- **Trailing Stop / Step (Pips)** – ermöglicht dynamischen Schutz, sobald der Trade den konfigurierten Gewinn-Schwellenwert erreicht. Der Step muss ungleich null sein, wenn das Trailing aktiv ist.
- **Max Position Volume** – begrenzt die absolute Netto-Position. Signale, die die Grenze verletzen würden, werden ignoriert.
- **Closing Mode** – bestimmt die Reaktion, wenn eine nicht konforme Kerze erscheint:
  - `All` – die gesamte Position flatten.
  - `Opposite` – Positionen gegen die Serienrichtung schließen (z.B. Shorts nach bärischer Laufunterbrechung).
  - `Unidirectional` – nur Positionen in der Serienrichtung schließen.
- **Handelsfenster** – die Strategie öffnet neue Trades nur, wenn die Kerzeneröffnungszeit-Stunde zwischen `StartHour` und `EndHour` (inklusiv) liegt. Schutzausstiege funktionieren auch wenn neue Trades gesperrt sind.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `CandlesCount` | 3 | Anzahl der identischen Kerzen die für ein Signal erforderlich sind. |
| `OrderVolume` | 0.01 | Basis-Marktordergröße. Entgegengesetzte Exposition wird vor dem Eröffnen eines neuen Trades geschlossen. |
| `TakeProfitPips` | 50 | Take-Profit-Abstand in Pips. `0` deaktiviert das Ziel. |
| `StopLossPips` | 50 | Stop-Loss-Abstand in Pips. `0` deaktiviert den Stop. |
| `TrailingStopPips` | 10 | Trailing-Stop-Abstand in Pips. `0` deaktiviert das Trailing. |
| `TrailingStepPips` | 4 | Mindestpreisverbesserung bevor das Trailing-Niveau sich bewegt. Muss > 0 sein wenn Trailing aktiviert ist. |
| `MaxPositionVolume` | 2 | Maximale absolute Netto-Position. |
| `UseTradingHours` | true | Aktiviert Handelsfenster-Filterung. |
| `StartHour` | 11 | Beginn der Handelssession (0-23). |
| `EndHour` | 18 | Ende der Handelssession (0-23). |
| `ClosingMode` | All | Verhalten wenn eine schwarze Schaf-Kerze erscheint. |
| `CandleType` | 1-Stunden-Kerzen | Für die Signalgenerierung verwendeter Datentyp. |

## Hinweise
- Die Pip-Konvertierung basiert auf dem Instrument-`PriceStep`. Für 5- und 3-Ziffern-Notierungen multipliziert die Strategie den Schritt mit zehn, um der traditionellen Pip-Definition zu entsprechen.
- `StartProtection()` während des Starts aufrufen, um StockSharp-Sicherheitsdienste zu aktivieren (Stornierung-bei-Stop, Wiederverbindungs-Sicherheit, etc.).
- Die Logik verwendet die Netto-Position (`Strategy.Position`) und operiert daher korrekt auf Netting-Konten. Hedging-ähnliches Verhalten kann durch Setzen eines hohen `MaxPositionVolume` emuliert werden.
