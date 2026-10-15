# TradeXpert Manuelles Handelspanel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Der ursprüngliche TradeXpert MQL5-Experten-Advisor ist ein manuell bedientes Handelspanel, das eine Sammlung von Schaltflächen zum Öffnen von Positionen, Platzieren ausstehender Orders, Anwenden von Schutz-Stops und schnellem Umkehren oder Schließen eines bestehenden Trades bereitstellt. Dieser C#-Port reproduziert dasselbe Toolkit innerhalb von StockSharp, indem jede Panel-Aktion in einen Strategieparameter umgewandelt wird. Die Strategie selbst generiert keine Handelssignale; stattdessen wartet sie auf Ihre manuellen Anweisungen, führt die angeforderten Orders aus und überwacht Schutzausstiege im eingehenden Kerzen-Flow.

## Nachgebildete Funktionalität
- **Marktaktionen.** Einmalige Anfragen für `Buy`- oder `Sell`-Marktorders unter Verwendung des konfigurierten Handelsvolumens.
- **Ausstehende Orders.** Einmalige Platzierung von Buy Limit/Stop- und Sell Limit/Stop-Orders mit einem absoluten Preis oder einem Versatz vom letzten Kerzenschlusskurs.
- **Schutzverwaltung.** Stop-Loss- und Take-Profit-Niveaus können entweder als absolute Preisniveaus oder als Versätze vom aufgezeichneten Einstiegspreis definiert werden. Die Strategie überwacht Kerzenextrema und schließt die Position mit einer Marktorder, wenn ein Schutzniveau verletzt wird.
- **Manuelle Ausstiegskontrollen.** Dedizierte Parameter replizieren die Schließen- und Umkehren-Schaltflächen aus dem MQL-Panel und ermöglichen das Schließen oder Umkehren einer Position auf Abruf.

## Strategielogik
1. Die Strategie abonniert den durch `CandleType` angegebenen Kerzentyp. Der Stream wird verwendet, um den aktuellsten Schlusskurs für Versätze zu bestimmen und zu erkennen, ob Schutzniveaus überschritten wurden.
2. Bei jeder abgeschlossenen Kerze führt die Strategie folgendes durch:
   - Wendet das neueste `TradeVolume` auf die `Volume`-Eigenschaft der Basisklasse an.
   - Verarbeitet manuelle Schließen- oder Umkehrenanfragen, auch wenn noch keine Indikatoren gebildet wurden.
   - Sobald Marktdaten als bereit bestätigt sind, führt sie ausstehende Einstiegsanfragen aus, registriert ausstehende Orders und wertet Stop-Loss-/Take-Profit-Auslöser aus.
3. Wenn sich eine Positionsgröße ändert (neuer Einstieg, Skalieren oder Reduzierung), aktualisiert die Strategie den gespeicherten Einstiegspreis, sodass versatzbasierte Stops sofort den neuesten Trade widerspiegeln.
4. Die Schutzlogik verwendet das Kerzenhoch/-tief, um Verletzungen zu identifizieren. Wenn ein Niveau überschritten wird, wird eine Marktorder in der entgegengesetzten Richtung mit der aktuellen absoluten Positionsgröße gesendet, um sicherzustellen, dass die Position vollständig geschlossen wird.

## Parameter
- **`CandleType`** – Kerzenserie, die zur Überwachung von Preisen auf Versätze und Risikoprüfungen verwendet wird.
- **`TradeVolume`** – Volumen, das auf jede Markt- und ausstehende Order angewendet wird (muss positiv sein).
- **`EntryAction`** – Momentanselektor mit den Werten `None`, `BuyMarket` oder `SellMarket`. Das Setzen eines von `None` abweichenden Wertes löst die entsprechende Marktorder genau einmal aus und setzt dann auf `None` zurück.
- **`PendingAction`** – Selektor für ausstehende Orders (`None`, `BuyLimit`, `BuyStop`, `SellLimit`, `SellStop`). Die Aktion wird verbraucht, nachdem eine gültige Order registriert wurde.
- **`PendingPrice`** – Absoluter Preis für die ausstehende Order. Bei `0` lassen, um auf `PendingOffset` zurückzugreifen.
- **`PendingOffset`** – Versatz, der auf den letzten Kerzenschlusskurs angewendet wird, wenn `PendingPrice` null ist. Positive Versätze passen den Preis je nach ausgewählter Aktion automatisch über/unter den Schlusskurs an.
- **`UseStopLoss`** / **`StopLossPrice`** / **`StopLossOffset`** – Stop-Loss-Schutz aktivieren und konfigurieren. Versätze werden vom gespeicherten Einstiegspreis gemessen, wenn der absolute Preis nicht angegeben wird.
- **`UseTakeProfit`** / **`TakeProfitPrice`** / **`TakeProfitOffset`** – Analoge Einstellungen für Take-Profit-Management.
- **`ClosePositionRequest`** – Auf `true` setzen, um einen sofortigen Marktausstieg für die gesamte Position auszuführen. Das Flag wird auf `false` zurückgesetzt, nachdem die Anfrage verarbeitet wurde.
- **`ReversePositionRequest`** – Auf `true` setzen, um das aktuelle Exposure umzukehren. Die Strategie schließt die bestehende Position und eröffnet eine entgegengesetzte mit `ReverseVolume`, dann setzt sie das Flag zurück.
- **`ReverseVolume`** – Volumen der neuen Position, die nach einer Umkehrung eingerichtet wird. Wenn die Umkehrgröße der bestehenden Position entsprechen soll, setzen Sie es gleich der aktuellen absoluten Position.

## Verwendungsrichtlinien
1. Wählen Sie die Kerzenagregation (`CandleType`), die damit übereinstimmt, wie Sie Versätze und Risiko messen möchten. Der Standard-1-Minuten-Zeitrahmen spiegelt das ursprüngliche Panel-Verhalten wider, das auf eingehende Ticks reagierte.
2. Konfigurieren Sie `TradeVolume` und optionale Schutzniveaus (`StopLoss*`, `TakeProfit*`). Sie können frei zwischen absoluten Niveaus und Versätzen wechseln; die Versätze aktivieren sich immer dann, wenn der absolute Wert bei null belassen wird.
3. Für ausstehende Orders entscheiden Sie, ob Sie einen festen Preis (`PendingPrice`) oder einen Versatz vom letzten Schlusskurs (`PendingOffset`) bevorzugen. Die Strategie berechnet den Preis zu dem Zeitpunkt neu, zu dem die Order eingereicht wird.
4. Senden Sie Handelsanweisungen durch Ändern von `EntryAction`, `PendingAction`, `ClosePositionRequest` oder `ReversePositionRequest`. Jeder Parameter verhält sich wie eine Schaltfläche: Sobald die Anfrage ausgeführt ist, setzt sich der Wert automatisch zurück, damit die Aktion bei der nächsten Kerze nicht wiederholt wird.
5. Die Strategie überwacht weiterhin die Preisaktion, während eine Position offen ist. Immer wenn ein Stop-Loss- oder Take-Profit-Schwellenwert überschritten wird, wird die Position mit einer Marktorder geschlossen; beide Schutzauslöser werden bis zum nächsten Einstieg deaktiviert, um doppelte Orders zu vermeiden.

## Unterschiede zur ursprünglichen MQL-Version
- Das visuelle Panel wird durch Strategieparameter ersetzt. Jede Schaltfläche aus der ursprünglichen UI ist jetzt als Schalter oder Selektor verfügbar, der aus dem StockSharp-Parameterraster oder Automatisierungsskripten bearbeitet werden kann.
- Anstatt Stop- oder Limit-Orders für Schutz zu platzieren, schließt die Strategie die Position mit Marktorders, wenn die angegebenen Preisniveaus verletzt werden. Dies hält die Implementierung kompatibel mit der High-Level-API und vermeidet die Verwaltung separater Stop-Orders.
- Preisversätze verwenden abgeschlossene Kerzen anstelle von rohen Ticks. Dies hält das Verhalten deterministisch über Backtests und Live-Handelssitzungen hinweg und liefert gleichzeitig Intraday-Reaktionsfähigkeit.

## Hinweise
- Sie können mehrere Anweisungen innerhalb derselben Kerze in die Warteschlange stellen (zum Beispiel eine Marktkaufsanfrage stellen und sofort einen Take-Profit-Versatz anfordern). Die Strategie verarbeitet sie sequentiell auf der nächsten abgeschlossenen Kerze.
- Wenn Sie dieselbe Aktion erneut ausgeben müssen, wählen Sie einfach den gewünschten Wert erneut; die interne Tracking-Logik erkennt die Änderung und führt die neue Anfrage aus.
- Beim Skalieren in eine Position wird der gespeicherte Einstiegspreis auf den Schlusskurs der Kerze aktualisiert, die die neue Größe widerspiegelt. Passen Sie die Versätze entsprechend an, wenn Sie genaue Schutzabstände benötigen.
