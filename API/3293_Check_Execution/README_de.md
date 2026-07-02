# Check-Execution-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Check-Execution-Strategie reproduziert das Verhalten des ursprünglichen MQL-Experts, der eine Brokerorder wiederholt modifiziert, um Ausführungsqualität zu messen. Der Algorithmus kann entweder einen Pending Buy Stop oder einen schützenden Sell Stop testen, der eine mit Marktorder eröffnete Long-Position absichert. Jede Modifikation zeichnet sowohl den beobachteten Spread als auch die Zeit auf, die der Handelsplatz benötigt, um die Änderung zu akzeptieren. Dadurch lassen sich latenzsensitive Bedingungen eines Brokers einfach bewerten.

## Kernlogik
1. Beste Bid-/Ask-Aktualisierungen über die High-Level-API `SubscribeLevel1` abonnieren.
2. Die initiale Testorder abhängig vom gewählten Modus platzieren:
   - **Pending** - einen Buy Stop oberhalb des aktuellen Ask-Preises senden.
   - **Market** - zum Markt kaufen und anschließend einen schützenden Sell Stop unterhalb des letzten Ask senden.
3. Bei jeder Kursaktualisierung:
   - Den rollierenden Durchschnitt des Bid/Ask-Spreads mit `SimpleMovingAverage` aktualisieren.
   - Die verfolgte Order mit neuem Offset vom Ask-Preis erneut registrieren, wenn eine Änderung nötig ist und keine vorherige Anfrage auf Bestätigung wartet.
   - Die Ausführungslatenz messen, sobald die Order in den Zustand `Active` zurückkehrt, und sie in einen zweiten `SimpleMovingAverage` einspeisen, um die laufende durchschnittliche Verzögerung in Millisekunden zu erhalten.
4. Den Modifikationszyklus wiederholen, bis die konfigurierte Iterationszahl erreicht ist. Danach storniert die Strategie verbleibende Pending-/Stop-Orders, schließt bei Bedarf die eröffnete Long-Position und gibt aggregierte Spread- und Latenzstatistiken aus.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Volume` | Handelsvolumen für jede Order. | `0.01` |
| `Iterations` | Anzahl der Modifikationsversuche für die Mittelwertbildung. Auf 1-500 begrenzt. | `30` |
| `Order Mode` | Wählt den Ablauf: `Pending` oder `Market`. | `Pending` |
| `Pending Offset` | Distanz in Preisschritten über dem Ask für den Test-Buy-Stop. | `100` |
| `Stop Offset` | Distanz in Preisschritten unter dem Ask für den schützenden Sell Stop. | `100` |

## Verhaltenshinweise
- Volumenwerte werden auf die `VolumeStep`-, `MinVolume`- und `MaxVolume`-Beschränkungen des Wertpapiers normalisiert, um abgelehnte Orders zu vermeiden.
- Preis-Offsets werden mit dem Instrumenten-`PriceStep` in tatsächliche Preise übersetzt. Ein Standardschritt von `0.0001` wird verwendet, wenn das Wertpapier keinen bereitstellt.
- Die Strategie zählt eine Modifikation nur, wenn der Handelsplatz die Anfrage bestätigt, indem er die Order in den Zustand `Active` oder `Done` bewegt. Jede Bestätigung aktualisiert sowohl den Ausführungstimer als auch den Modifikationszähler.
- Sobald die Zielanzahl von Iterationen erreicht ist, beendet die Strategie automatisch Orderänderungen, storniert Pending-Schutz, schließt jede Testposition und protokolliert eine Zusammenfassung mit den gemessenen Mittelwerten.

## Unterschiede zur MQL-Version
- Spread- und Ausführungsdurchschnitte werden mit StockSharp-`SimpleMovingAverage`-Indikatoren statt manuellen Arrays berechnet.
- Ordermanagement verwendet High-Level-Helfer wie `BuyMarket`, `BuyStop`, `SellStop` und `ReRegisterOrder`, um mit dem StockSharp-Strategieframework konsistent zu bleiben.
- Benutzeroberflächenfeedback wird über das Strategielog statt über Chart-Kommentare und grafische Objekte bereitgestellt.
