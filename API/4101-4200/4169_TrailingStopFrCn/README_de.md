# TrailingStopFrCn-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

`TrailingStopFrCnStrategy` ist ein StockSharp-Port des MetaTrader-Expertenberaters **TrailingStopFrCn.mq4**. Das ursprüngliche Skript verwaltet Stop-Loss-Level für bestehende Positionen mithilfe einer Mischung aus festen Trailing-Distanzen, Bill Williams-Fraktalen oder aktuellen Kerzenhochs/-tiefs. Dieser Port behält die gleiche Flexibilität bei, während er mit dem High-Level-StockSharp API integriert ist: Die Strategie abonniert Kerzen und Level-1-Kurse, überwacht die aktuelle Nettoposition und aktualisiert automatisch eine schützende Stop-Order.

Im Gegensatz zu einer Einstiegsstrategie konzentriert sich TrailingStopFrCn ausschließlich auf das Risikomanagement. Es werden keine neuen Stellen eröffnet. Stattdessen verfolgt es die bestehende Position von `Strategy.Security`, storniert veraltete Stop-Orders, wenn die Position umkippt, und sendet eine einzelne aggregierte Stop-Order, die der Logik des MetaTrader-Beraters folgt.

## Nachgestellte Logik

1. **Fester Nachlaufabstand** – wenn `TrailingStopPips` größer als Null ist, verhält sich die Strategie wie der ursprüngliche MQL-Parameter `TrailingStop`. Für Long-Positionen wird der Stop bei `bestBid - distance` platziert, für Short-Positionen bei `bestAsk + distance`, mit `distance = TrailingStopPips × pip size`.
2. **Fraktal-Trailing** – Bei `TrailingStopPips = 0` und `TrailingMode = Fractals` erkennt die Strategie Bill-Williams-Fraktale mit fünf Balken. Jede fertige Kerze wird einem internen Puffer hinzugefügt und sobald genügend Verlauf verfügbar ist, wird die zwei Balken zurückliegende Kerze als potenzielles Fraktal bewertet. Das jüngste Fraktal, das mindestens `MinStopDistancePips` vom aktuellen Preis entfernt ist, wird zum neuen Stoppkandidaten.
3. **Kerzennachlauf** – bei `TrailingStopPips = 0` und `TrailingMode = Candles` durchsucht die Strategie die letzten 99 geschlossenen Kerzen und wählt das erste Tief (für Long-Positionen) oder Hoch (für Short-Positionen) aus, das mindestens `MinStopDistancePips` vom aktuellen Preis entfernt ist.

Nach der Berechnung der Kandidatenebene erzwingt die Strategie dieselben Schutzregeln wie die MQL-Version:

- **OnlyProfit** verhindert das Verschieben des Stops, es sei denn, das neue Niveau würde einen Gewinn sichern (Stopp über dem Einstiegspunkt für Long-Positionen, Stop unter dem Einstiegspunkt für Short-Positionen).
- **OnlyWithoutLoss** stoppt das weitere Trailing, sobald der aktive Stop-Loss die Position bereits vor Verlusten schützt (im Originalskript stoppt der Trailing-Prozess, nachdem die Gewinnschwelle erreicht ist).
- Der Stop wird nur in die günstige Richtung bewegt: nach oben für Long-Positionen und nach unten für Short-Positionen.

Da StockSharp eine einzelne Nettoposition pro Wertpapier verfolgt, entspricht das Stop-Order-Volumen `Math.Abs(Position)` und alle zugrunde liegenden Ausführungen werden aggregiert.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `OnlyProfit` | Verschieben Sie den Stop-Loss nur, wenn das neue Niveau einen Gewinn im Verhältnis zum durchschnittlichen Einstiegspreis sichert. Spiegelt das Flag `OnlyProfit` von MQL. |
| `OnlyWithoutLoss` | Stoppen Sie das Trailing, sobald der aktive Stop-Loss den Einstiegspreis erreicht oder überschreitet. Dadurch wird `OnlyWithoutLoss` vom ursprünglichen Advisor repliziert. |
| `TrailingStopPips` | Feste Nachlaufdistanz, ausgedrückt in Pips. Auf Null setzen, um Fraktal- oder Candle-Trailing zu aktivieren. |
| `MinStopDistancePips` | Minimaler Abstand (in Pips) zwischen Marktpreis und Stop-Loss. Verwenden Sie es, um die Broker-`MODE_STOPLEVEL`-Einschränkung zu emulieren. |
| `TrailingMode` | Wählt die nachgestellte Quelle aus, wenn `TrailingStopPips = 0`. Optionen: `Fractals` (Bill Williams Fünf-Balken-Fraktale) oder `Candles` (aktuelle Tiefs/Hochs). |
| `CandleType` | Kerzendatentyp, der zum Erstellen von Fraktalen oder zum Suchen nach Swing-Punkten verwendet wird. Der Standardwert ist ein Zeitrahmen von einer Stunde. |

## Verhaltensnotizen

- Die Strategie abonniert Daten der Ebene 1, um auf die besten Geld-/Briefkurse zuzugreifen. Das Trailing mit fester Distanz reagiert sofort auf Aktualisierungen der Stufe 1, während das Fraktal-/Kerzen-Trailing aktualisiert wird, wenn neue Kerzen eintreffen.
- Wenn sich die Positionsrichtung ändert, wird die aktuelle Stop-Order storniert, bevor die neue Order übermittelt wird.
- Wenn kein Stop-Kandidat verfügbar ist (z. B. nicht genügend Kerzen), behält die Strategie den vorhandenen Stop bei.
- Wenn der Broker keinen Mindeststoppabstand vorschreibt, können Sie `MinStopDistancePips` auf Null belassen.

## Unterschiede zur MetaTrader-Version

- StockSharp behält eine Nettoposition bei, daher werden einzelne MetaTrader „Tickets“ nicht verfolgt. Die Stop-Order deckt die gesamte aggregierte Position ab.
- Der `Magic`-Filter ist nicht erforderlich: Die Strategie arbeitet bereits mit ihrem eigenen Sicherheitskontext.
- Nachfolgende Aktualisierungen werden durch fertige Kerzen plus Level-1-Daten gesteuert und nicht durch eine einsekündige Abfrageschleife.
- Visuelle Diagrammobjekte aus dem Original EA werden nicht neu erstellt; Stattdessen können Sie beim Ausführen der Beispiel-Benutzeroberfläche die Diagrammhilfsprogramme von StockSharp verwenden.

## Anwendungstipps

1. Führen Sie die Strategie zusammen mit jeder Einstiegslogik aus, die Positionen auf demselben `Security` eröffnet. TrailingStopFrCn fügt automatisch eine Stop-Order hinzu, sobald die Position erscheint.
2. Passen Sie `CandleType` an den Zeitrahmen an, der auf Fraktale oder Swing-Punkte analysiert werden soll. Höhere Zeitrahmen glätten nachlaufende Niveaus, während niedrigere Zeitrahmen schneller reagieren.
3. Kalibrieren Sie `MinStopDistancePips` entsprechend den Stop-Level-Beschränkungen Ihres Brokers. Eine zu niedrige Einstellung kann zur Ablehnung von Bestellungen führen.
4. Stellen Sie beim Testen historischer Daten sicher, dass Kerzenabonnements und Level-1-Nachrichten in der Datenquelle verfügbar sind, damit die nachgestellte Logik korrekt ausgelöst werden kann.
