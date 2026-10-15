# Martingale MA Ausbruch-Strategie (ID 2861)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Martingale MA Ausbruch-Strategie ist eine Portierung des ursprünglichen MetaTrader 5-Expertenberaters `Martingale.mq5`. Sie überwacht, wie weit sich der aktuelle Preis von einem gleitenden Durchschnitt auf einem höheren Zeitrahmen entfernt. Wenn die Distanz eine konfigurierbare Anzahl von Pips überschreitet, öffnet die Strategie eine neue Position in Richtung der Bewegung und verwaltet sie mit fester Stop-Loss-, Take-Profit- und Trailing-Logik. Die Positionsgröße folgt einer Martingale-artigen Anpassung, die die Handelsgröße nach Verlustseguenzen erhöht und nach rentablen Perioden reduziert.

Standardmäßig bewertet die Strategie 6-Minuten-Kerzen, während die umgebende Plattform auf jedem Basiszeitrahmen betrieben werden kann. Alle Indikatorberechnungen werden am ausgewählten Kerzentyp durchgeführt, während Aufträge mit Marktausführung gesendet werden.

## Handelslogik

1. Den gleitenden Durchschnittswert für die aktuelle Kerze mit der ausgewählten Glättungsmethode, dem angewandten Preis und der Verschiebung berechnen.
2. Den konfigurierten Pip-Abstand in ein absolutes Preis-Delta umwandeln. Die Pip-Größe repliziert die ursprüngliche MQL-Abstimmung: Symbole mit 3 oder 5 Dezimalstellen multiplizieren den Preisschritt mit 10.
3. Wenn die Kerze schließt:
   - Wenn der Schluss mehr als `DistanceFromMaPips` Pips über dem verschobenen gleitenden Durchschnitt liegt und keine aktive Long-Exposition vorhanden ist, einen Markt-Kaufauftrag senden.
   - Wenn der Schluss mehr als `DistanceFromMaPips` Pips unter dem verschobenen gleitenden Durchschnitt liegt und keine aktive Short-Exposition vorhanden ist, einen Markt-Verkaufsauftrag senden.
4. Jede abgeschlossene Kerze aktualisiert auch den Trailing-Stop und prüft, ob der Schlusskurs den simulierten Stop-Loss oder Take-Profit verletzt. Das Schließen einer Position löst `ResetTradeState` aus und löscht alle gespeicherten Niveaus.

## Geldmanagement

- `RiskPercent` wandelt in ein monetäres Risikobudget um und verwendet `Portfolio.CurrentValue` (oder `BeginValue`, wenn keine Trades gemacht wurden). Wenn ein Stop-Loss angegeben ist, schätzt das Budget dividiert durch die Stop-Distanz und den Sicherheitsmultiplikator das maximale erschwingliche Volumen.
- Nach der Größenbestimmung durch Risiko wird das Volumen durch `ApplyMartingale` geleitet: Wenn der letzte aufgezeichnete Kontostand (nach dem vorherigen Einstieg erfasst) höher als der aktuelle Kontostand ist, erhöht sich das Volumen um 1 Einheit; wenn er niedriger ist, sinkt das Volumen um 1 Einheit, fällt aber nie unter das Basisvolumen der Strategie.
- Die Trailing-Logik ahmt den ursprünglichen EA nach: Sobald der Preis sich um `TrailingStopPips + TrailingStepPips` zugunsten der Position bewegt, wird der Stop gezogen, um den `TrailingStopPips`-Offset beizubehalten. Die Strategie validiert, dass `TrailingStepPips` beim aktivierten Trailing ungleich null ist, und spiegelt die MQL-Fehlerbehandlung wider.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `StopLossPips` | Stop-Loss-Abstand in Pips. Ein Wert von null deaktiviert den Stop und die risikobasierte Größenbestimmung. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Null zum Deaktivieren. |
| `TrailingStopPips` | Trailing-Stop-Offset in Pips. Muss mit `TrailingStepPips` gepaart werden. |
| `TrailingStepPips` | Zusätzliche Preisbewegung, bevor der Trailing-Stop vorgerückt wird. Kann nicht null sein, wenn Trailing aktiv ist. |
| `DistanceFromMaPips` | Minimale Distanz zwischen Preis und verschobenem gleitenden Durchschnitt, die Einstiege auslöst. |
| `CandleType` | Datentyp für Indikatorberechnungen (Standard 6-Minuten-Zeitrahmen). |
| `MaPeriod` | Zeitraum des gleitenden Durchschnitts. |
| `MaShift` | Anzahl der Balken, um die der gleitende Durchschnitt vorwärts verschoben ist. Die Strategie speichert historische MA-Werte, um das MQL-Verhalten zu emulieren. |
| `MaMethod` | Glättungstyp des gleitenden Durchschnitts: Einfach, Exponentiell, Geglättet oder Gewichtet. |
| `MaAppliedPrice` | Kerzenpreis für den gleitenden Durchschnitt (Schluss, Eröffnung, Hoch, Tief, Median, typisch oder gewichtet). |
| `RiskPercent` | Prozentsatz des aktuellen Eigenkapitals, das dem Stop-Loss-Risikobudget zugewiesen wird. |

## Ausführungshinweise

- Die Strategie arbeitet ausschließlich an abgeschlossenen Kerzen, um die "neue Balken"-Verarbeitung des ursprünglichen EA zu replizieren. `BuyMarket`/`SellMarket` kippt die bestehende Exposition, indem der Absolutwert der entgegengesetzten Position hinzugefügt wird.
- Stops und Ziele werden im Code simuliert, da StockSharp sie in dieser Konvertierung nicht automatisch verwaltet. Der Schlusskurs wird als Proxy für die Tick-Level-Ausführung verwendet.
- Martingale-Anpassungen operieren auf dem Kontostands-Snapshot, der unmittelbar nach jedem Einstieg aufgenommen wurde, ähnlich dem Quell-EA.
- Wenn dem Symbol ein gültiger Preisschritt oder Multiplikator fehlt, werden Standardwerte von `0.0001` und `1` verwendet, um Divisionsfehler zu vermeiden.

## Unterschiede vom Original-EA

- Die MQL-Version verwendete Bid/Ask-Preise; dieser Port arbeitet mit Kerzenschlusspreisen, da hochfrequente Ticks in der High-Level-API nicht verfügbar sind.
- Die Volumengröße basiert auf Portfolio-Eigenkapital und Sicherheitsmultiplikator anstelle des `CMoneyFixedMargin`-Helpers.
- Die Diagrammvisualisierung ist optional: Wenn ein Diagrammbereich verfügbar ist, zeichnet die Strategie Kerzen; standardmäßig werden keine zusätzlichen Indikatoren geplottet.
- Die Validierung, dass `TrailingStepPips` beim aktivierten Trailing positiv sein muss, löst beim Start eine Ausnahme aus, anstatt `Alert` aufzurufen.
