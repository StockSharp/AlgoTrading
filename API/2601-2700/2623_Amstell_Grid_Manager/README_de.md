# Amstell Grid-Manager-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

High-Level-Port des MetaTrader-Experten "exp_Amstell-SL", der ein bidirektionales Averaging-Grid betreibt. Die Strategie verfolgt den zuletzt ausgeführten Preis auf jeder Seite und gibt zusätzliche Marktaufträge aus, wenn der Preis weit genug driftet, während der offene Batch liquidiert wird, sobald eine feste Take-Profit- oder Stop-Loss-Distanz erreicht ist. Die Implementierung verwendet StockSharps Kerzensubskriptionen und High-Level-Order-Helfer, sodass sie in jede Umgebung eingesteckt werden kann, die Kerzendaten für ein einzelnes Wertpapier bereitstellt.

Die übersetzte Logik ist leicht an StockSharps Netto-Portfolio-Modell angepasst: Long- und Short-Grids werden weiterhin separat verwaltet, aber nicht gleichzeitig gehalten. Das Long-Grid ist aktiv, solange die Nettoposition nicht negativ ist, und das Short-Grid übernimmt erst, nachdem das gesamte Long-Engagement abgebaut wurde.

## Wie es funktioniert

### Marktdaten und Ausführungsfluss
- Abonniert den konfigurierten `CandleType` (Standard: 1-Minuten-Zeitrahmen-Kerzen) und verarbeitet nur abgeschlossene Kerzen.
- Berechnet Pip-basierte Offsets vom `PriceStep` des Wertpapiers. Wenn der Schritt 3 oder 5 Dezimalstellen hat, wird er mit 10 multipliziert, um die MetaTrader 3/5-Digit-Pip-Anpassung nachzuahmen.
- Alle Trades werden durch `BuyMarket`/`SellMarket`-Helfer platziert; es werden keine ausstehenden Aufträge verwendet.

### Long-Seiten-Management
- Öffnet die erste Long-Position (`OrderVolume`), sobald kein bestehendes Long-Engagement vorhanden ist und die Strategie nicht dabei ist, Shorts zu schließen.
- Verfolgt den zuletzt ausgeführten Long-Preis und den volumengewichteten durchschnittlichen Einstandspreis für den aktiven Long-Batch.
- Platziert zusätzliche Long-Aufträge der Größe `OrderVolume`, wenn der Schlusskurs um mindestens `BuyDistancePips` (in Preiseinheiten umgerechnet) unter dem letzten Long-Fill gefallen ist.

### Short-Seiten-Management
- Sobald der Long-Batch vollständig geschlossen ist und die Nettoposition nicht positiv ist, erlaubt die Strategie Short-Einstiege.
- Platziert die anfängliche Short-Order, wenn keine Short-Exposition vorhanden ist; weitere Shorts werden eröffnet, nachdem der Preis um `BuyDistancePips * SellDistanceMultiplier` über den vorherigen Short-Fill gestiegen ist.
- Verwaltet den zuletzt ausgeführten Short-Preis und den volumengewichteten durchschnittlichen Einstandspreis für den aktiven Short-Batch.

### Ausstiegsregeln
- Berechnet für jede Richtung den nicht realisierten Gewinn relativ zum durchschnittlichen Fill.
- Schließt den gesamten Long-Batch mit einem Marktverkauf, wenn der Gewinn `TakeProfitPips` Pips erreicht oder der Drawdown `StopLossPips` Pips erreicht.
- Schließt den gesamten Short-Batch mit einem Marktkauf, wenn der Gewinn `TakeProfitPips` Pips erreicht oder die adverse Bewegung `StopLossPips` Pips erreicht.
- Nach der Liquidation werden alle zwischengespeicherten Preise und Volumen zurückgesetzt, damit ein neues Grid mit der nächsten Kerze beginnen kann.

### Unterschiede zum originalen MQL-Experten
- Die StockSharp-Version arbeitet auf Kerzenabschlüssen anstatt auf einzelnen Ticks.
- Long- und Short-Grids werden sequentiell statt gleichzeitig ausgeführt, was dem Standard-Netting-Modus von StockSharp entspricht.
- Alle Schutzabstände werden gegen den gemittelten Einstandspreis statt gegen jedes einzelne Ticket geprüft, was das aggregierte Netto-Positionsverhalten widerspiegelt.

## Parameter

| Parameter | Standard | Optimierungsbereich | Beschreibung |
|-----------|---------|--------------------|-------------|
| `OrderVolume` | `0.01` | `0.01` – `0.10` (Schritt `0.01`) | Mit jeder Grid-Order eingereichte Menge. Muss positiv sein. |
| `TakeProfitPips` | `30` | `10` – `150` (Schritt `10`) | Gewinnziel für den aktiven Batch in Pips. |
| `StopLossPips` | `30` | `10` – `150` (Schritt `10`) | Maximale adverse Bewegung vor dem Aufgeben des Batches. |
| `BuyDistancePips` | `10` | `5` – `60` (Schritt `5`) | Minimaler Rückgang vom letzten Long-Fill zum Hinzufügen eines weiteren Kaufs. Muss kleiner als TP und SL sein. |
| `SellDistanceMultiplier` | `10` | `2` – `15` (Schritt `1`) | Multiplikator für die Long-Distanz beim Abstands-Management von Short-Einstiegen. |
| `CandleType` | 1-Minuten-Zeitrahmen | — | Kerzenserie für die Signalgenerierung. |

## Implementierungshinweise
- `BuyDistancePips` muss strikt kleiner als `TakeProfitPips` und `StopLossPips` sein; die Strategie wirft ansonsten beim Start eine Ausnahme und reproduziert damit die MetaTrader-Validierung.
- Die Pip-Größe wird vom `PriceStep` des Wertpapiers abgeleitet. Passen Sie die Parameter an, wenn das Instrument eine nicht standardmäßige Tick-Größe verwendet.
- Der gesamte interne Zustand wird in `OnReseted` gelöscht, sodass die Strategie ohne verbleibende Grid-Daten neu gestartet werden kann.
- Keine Farbanpassung oder manuelle Indikatorregistrierung wird verwendet, was den High-Level-API-Richtlinien in diesem Repository entspricht.
