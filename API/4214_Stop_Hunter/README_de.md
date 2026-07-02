# Stop-Hunter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Portiert den MetaTrader 4 Expertenberater **Stop Hunter** in das StockSharp High-Level-Strategie-Framework.
- Konzentriert sich auf Ausbrüche runder Zahlen: Der Algorithmus sucht ständig nach Preisniveaus, deren rechte `Zeroes`-Ziffern Null sind, und platziert Stop-Orders genau innerhalb dieser Schwellenwerte.
- Hält Take-Profit- und Stop-Loss-Level vor dem Broker verborgen, indem Exits intern überwacht werden, wodurch das „virtuelle“ Risikomanagement reproduziert wird, das im ursprünglichen EA verwendet wurde.
- Implementiert die zweistufige Skalierungslogik des Quellcodes: Der erste Teil einer Position wird nach dem ursprünglichen Ziel geschlossen, der Rest folgt über die doppelte Distanz.

## Datenfluss und Abonnements
1. Abonniert **Level1**-Daten (`SubscribeLevel1().Bind(ProcessLevel1)`) in `OnStarted`. Es ist nur der beste Bid/Ask-Stream erforderlich; Kerzen oder Indikatoren werden nicht verwendet.
2. Bei jedem Update werden die neuesten Geld- und Briefkurse gespeichert und die Entscheidungsmaschine ausgelöst, sobald die Strategie online ist und der Handel zulässig ist.
3. Zur Visualisierung eigener Trades wird ein optionaler Chartbereich erstellt, wenn die Strategie mit aktiviertem Charting ausgeführt wird.

## Logik der Auftragserteilung
- **Erkennung runder Füllstände**
  - Verwendet den Instrumentpreisschritt (`Security.PriceStep`) als MQL `Point`-Analogon.
  - Berechnet eine Rundenschrittlänge: `roundStep = PriceStep * 10^Zeroes`.
  - Berechnet die nächste runde Zahl über dem Gebot (`Math.Ceiling(bid / roundStep) * roundStep`).
  - Passt das Niveau an, wenn sich der Brief bereits im Puffer befindet, und spiegelt den ursprünglichen Schutz wider, der verhindert, dass Aufträge zu nahe am aktuellen Spread gesendet werden.
  - Leitet das untere Rundenniveau (`LevelS`) einen Rundenschritt unter `LevelB` ab und führt die gleiche Sicherheitsanpassung gegenüber dem Gebot durch.
- **Ausstehende Bestellungen**
  - Platziert einen **Kaufstopp** bei `LevelB - DistancePoints * PriceStep`, wenn keine bestehende Order aktiv ist, der Long-Handel aktiviert ist und keine Short-Position offen ist.
  - Platziert einen **Verkaufsstopp** symmetrisch bei `LevelS + DistancePoints * PriceStep`, wenn Short-Handel erlaubt ist und keine Long-Position besteht.
  - Storniert veraltete ausstehende Aufträge immer dann, wenn sich das berechnete Rundenziel nach vorne bewegt oder der Preis um mehr als einen Rundenschritt plus `DistancePoints * 50` abweicht, was der Bereinigungslogik aus der MQL-Version entspricht.
  - Hält die Gesamtzahl der aktiven Slots (Positionen + ausstehende Orders) innerhalb von `MaxLongPositions + MaxShortPositions`.

## Virtuelles Exit-Management
- Verfolgt den durchschnittlichen Einstiegspreis und das aktuelle Positionsvolumen.
- Verwendet zwei Ganzzahlakkumulatoren (`_takeProfitExtension`, `_stopLossExtension`), um die ursprünglichen versteckten Puffer zu reproduzieren:
  - Erstes Gewinnziel: Schließt die Hälfte der Position, wenn der Geld-/Briefkurs `TakeProfitPoints * PriceStep` zugunsten der Position erreicht.
  - Nach dem ersten teilweisen Ausstieg werden sowohl die Gewinn- als auch die Stop-Distanz um weitere `TakeProfitPoints`/`StopLossPoints` verlängert, wodurch die Phase des „zweiten Handels“ aktiviert wird.
  - Endgültiger Ausstieg: Schließt das verbleibende Volumen, entweder wenn das verdoppelte Ziel erreicht ist oder wenn die verdoppelte Stop-Loss-Distanz erreicht wird.
- Schließt am Markt mit `BuyMarket` oder `SellMarket` und spiegelt den EA wider, der Marktschlüsse anstelle von Stop-Loss-Orders auf Brokerseite ausgegeben hat.
- Entfernt den ausstehenden Stop auf der gegenüberliegenden Seite, wenn eine Position geöffnet wird, um eine Absicherung zu vermeiden, genau wie die ursprüngliche Schleife, die widersprüchliche Orders löschte.

## Money-Management
- Implementiert die Funktion `Call_MM()` aus EA erneut: `volume = balance / 100000 * RiskPercent`.
- Klemmt das berechnete Volumen zwischen `MinimumVolume` und `MaximumVolume` und rundet es auf den Lautstärkeschritt des Instruments (oder auf 2/1/0 Dezimalstellen, abhängig von `MinimumVolume`).
- Bei Teilausstiegen wird die aktuelle Positionsgröße erneut verwendet, um Halbwertsabschlüsse unter Berücksichtigung des Volumenschritts zu berechnen.

## Implementierungshinweise
- Verwendet nur StockSharp High-Level-APIs (`BuyStop`, `SellStop`, `BuyMarket`, `SellMarket`, Level1-Bindung). Es sind keine direkten Connectoraufrufe oder Indikatorsammlungen erforderlich.
- Behält den internen Status über Zurücksetzungen hinweg mit `ResetState()` bei und stellt sicher, dass Tabulatoren für die Einrückung gemäß den Repository-Richtlinien verwendet werden.
- Schutzklauseln (`IsFormedAndOnlineAndAllowTrading`) verhindern die Auftragsübermittlung, bevor die Strategie vollständig initialisiert ist.
- `OnOwnTradeReceived` spiegelt die MQL-Prüfungen wider, die erfolgreiche Schließungen bestätigten, bevor das Flag `SecondTrade` aktualisiert wurde.
- `OnOrderChanged` löscht Referenzen, um veraltete Handles zu verhindern, wenn Bestellungen storniert oder abgelehnt werden.

## Unterschiede zur MQL-Version
- Netting-Modell: StockSharp-Strategien arbeiten mit einer einzigen Nettoposition. Die Standardparameter ahmen immer noch den EA nach (ein langer und ein kurzer Slot), aber die Skalierung in mehrere gleichzeitige Tickets wird über die Nettoexposition hinaus nicht unterstützt.
- Die Risikoberechnung verwendet `Portfolio.CurrentValue` (Fallback auf `BeginValue`) anstelle von `AccountFreeMargin` und bietet so eine tragbare Näherung in Multi-Asset-Umgebungen.
- Virtuelle Stop-/Take-Profit-Abstände werden sauber zurückgesetzt, wenn ein neuer Trade eröffnet wird, wodurch der Akkumulationsfehler im historischen EA-Code vermieden wird.
- Alle Kommentare und Dokumentationen sind auf Englisch verfasst, während die README-Dateien die Strategie zusätzlich auf Russisch und Chinesisch beschreiben, wie es die Projektrichtlinien erfordern.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Zeroes` | 2 | Ziffern auf der rechten Seite, die Null sein müssen, damit ein Preis als runde Ebene betrachtet wird. |
| `DistancePoints` | 15 | Offset (in Preispunkten) zwischen dem Rundenniveau und dem Stop-Eintrag. |
| `TakeProfitPoints` | 15 | Versteckte Take-Profit-Distanz in Punkten. Wird auch für die zweite Erweiterungsstufe wiederverwendet. |
| `StopLossPoints` | 15 | Versteckte Stop-Loss-Distanz in Punkten (verdoppelt nach dem ersten Scale-Out). |
| `EnableLongOrders` | wahr | Ermöglicht Buy-Stop-Platzierung. |
| `EnableShortOrders` | wahr | Ermöglicht die Platzierung eines Verkaufsstopps. |
| `RiskPercent` | 5 | Prozentsatz des Kapitals, der zur Größe der ausstehenden Aufträge verwendet wird. |
| `MinimumVolume` | 0,1 | Mindestbestellgröße nach Rundung. |
| `MaximumVolume` | 30 | Obergrenze für das berechnete Volumen. |
| `MaxLongPositions` | 1 | Maximale Anzahl langer Slots (Position + ausstehend). |
| `MaxShortPositions` | 1 | Maximale Anzahl kurzer Slots (Position + ausstehend). |

## Nutzungstipps
1. Wählen Sie ein Instrument, dessen Preisschritt mit der MQL `Point`-Definition des ursprünglichen Fachberaters übereinstimmt. Forex-Paare mit gebrochenen Pips erfordern normalerweise `Zeroes = 2`.
2. Überwachen Sie die Tick-Größe und den Volumenschritt des Brokers. Durch Anpassen von `MinimumVolume` wird sichergestellt, dass die Rundungslogik den Austauschbeschränkungen entspricht.
3. Da Exits virtuell sind, sollten Sie die Strategie immer online halten, um zu vermeiden, dass Stop-Loss-Bedingungen verpasst werden. Erwägen Sie eine Kombination mit StockSharps `StartProtection()`, wenn ein börsenseitiges Risikomanagement erforderlich ist.
4. Sehen Sie sich die russischen und chinesischen README-Varianten an, um lokalisierte Erklärungen zu erhalten, die Händler mit verschiedenen Teams teilen können.
