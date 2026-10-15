# EES-Hedger (erweitert)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie spiegelt das Verhalten des klassischen MetaTrader „EES Hedger“-Expertenberaters wider. Immer wenn ein externer Händler, diskretionärer Betreiber oder ein anderes automatisiertes System eine Position auf demselben Konto eröffnet, erstellt die Strategie sofort eine gegenläufige Absicherung mit einem konfigurierbaren Volumen. Anschließend verwaltet es die Absicherung mit Stop-Loss-, Take-Profit-, Break-Even- und Trailing-Stop-Regeln, sodass das Risiko neutralisiert und gleichzeitig die Gewinne aus der Absicherung geschützt werden.

Im Gegensatz zu herkömmlichen signalgesteuerten Strategien geht dieses Modul davon aus, dass Einträge an anderer Stelle erstellt werden. Seine alleinige Verantwortung besteht darin, Kontogeschäfte zu beobachten, auf übereinstimmende Tickets zu reagieren und die Absicherungsposition zu schützen, bis sie entweder durch Schutzaufträge oder manuell geschlossen wird.

## Handelslogik

1. **Erkennung externer Trades** – der Connector-Strom der Konto-Trades wird überwacht. Geschäfte, deren Kommentar mit `OriginalOrderComment` übereinstimmt (oder alle Geschäfte, wenn das Feld leer ist), werden als die Quelle behandelt, die abgesichert werden muss. Durch die Strategie selbst erzeugte Trades werden durch die Speicherung ihrer Transaktionskennungen gefiltert.
2. **Spiegelungsaufträge** – Sobald ein qualifizierter Handel eingeht, übermittelt die Strategie sofort einen Marktauftrag in die entgegengesetzte Richtung mit einem Volumen von `HedgeVolume`. Ein optionales `HedgerOrderComment` hilft Backoffice-Tools dabei, die Sicherungsaufträge von anderen Aktivitäten zu trennen.
3. **Risikomanagement** – nachdem die Absicherung erfüllt ist, platziert die Strategie Stop-Loss- und Take-Profit-Orders in Abständen, die durch die Pip-Parameter definiert sind. Wenn die Break-Even-Bedingungen erfüllt sind, wird der Stop auf den Einstiegspreis plus einen Pip verschoben. Wenn das Trailing aktiviert ist, wird der Stop weiter vorgezogen, da sich der Markt weiterhin zugunsten der Absicherung bewegt.
4. **Statusbereinigung** – wenn die Position Null erreicht (z. B. nach einem manuellen Abschluss), werden alle Schutzaufträge storniert und interne Flags zurückgesetzt, sodass der nächste externe Handel von Grund auf abgesichert werden kann.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `HedgeVolume` | Volumen, das zur Eröffnung der entgegengesetzten Hedge-Position verwendet wird. |
| `StopLossPips` | Abstand vom Einstiegspreis zur schützenden Stop-Loss-Order. |
| `TakeProfitPips` | Abstand vom Einstiegspreis bis zur Take-Profit-Order. |
| `TrailingStopPips` | Abstand, der vom Trailing Stop eingehalten wird, sobald die Aktivierungsschwelle überschritten wird. Auf Null setzen, um das Nachziehen zu deaktivieren. |
| `TrailingActivationPips` | Mindestgewinn (in Pips), der erforderlich ist, bevor sich der Trailing Stop zu bewegen beginnt. |
| `BreakEvenPips` | Gewinnschwelle (in Pips), nach der der Stop-Loss auf den Einstiegspreis plus einen Pip verschoben wird. |
| `OriginalOrderComment` | Optionaler Kommentarfilter, der auswählt, welche externen Geschäfte abgesichert werden sollen. Lassen Sie dieses Feld leer, um alle Trades des Instruments abzusichern. |
| `HedgerOrderComment` | Kommentar zu Absicherungsaufträgen und Schutzstopps, die durch die Strategie generiert werden. |

## Praktische Hinweise

- Weisen Sie der Strategie dasselbe Portfolio/Konto zu wie der externe Händler. Alle auf diesem Konto erstellten Positionen sind für den Connector sichtbar und können daher abgesichert werden.
- Bei Verwendung mit MetaTrader-Brücken konfigurieren Sie den Expertenberater oder die Brücke so, dass der ursprüngliche Bestellkommentar kopiert wird, damit die Filterung wie erwartet funktioniert.
- Die Pip-Größe wird aus der Preisstufe des Instruments abgeleitet. Bei fünfstelligen FX-Symbolen übersetzt der Abstand automatisch die angegebenen Pip-Werte in korrekte Preis-Offsets.
- Break-Even- und Trailing-Logik bewegen den Stop niemals weiter vom Einstiegspreis weg. Es werden nur Verbesserungen vorgenommen, um sicherzustellen, dass der Stopp nach Erreichen der Gewinnschwelle nie wieder auf ein Verlustniveau zurückfällt.
- Die Strategie verwaltet nicht die ursprüngliche Position. Die Schließung oder Änderung liegt weiterhin in der Verantwortung des primären Handelssystems.

## Nutzungsworkflow

1. Konfigurieren Sie die Strategieparameter und achten Sie dabei besonders auf die Kommentarfilter und das Volumen der Absicherung.
2. Starten Sie die Strategie und bestätigen Sie, dass sie mit dem Broker-Feed verbunden ist. Es bleibt inaktiv, bis ein externer Handel eintrifft.
3. Sobald ein qualifizierter Trade erscheint, beobachten Sie, wie die Absicherungsorder erstellt und wie Schutzaufträge im DOM platziert werden.
4. Überwachen Sie das Break-Even- und Trailing-Verhalten, um sicherzustellen, dass die konfigurierten Pip-Abstände mit den Vertragsspezifikationen des Brokers übereinstimmen.
5. Stoppen Sie die Strategie, wenn keine Absicherung mehr erforderlich ist. Während des Shutdowns werden alle aktiven Schutzanordnungen aufgehoben.

## Einschränkungen

- Das Modul übernimmt den Zugriff auf den Handelsstrom des Kontos. Es kann keine Geschäfte absichern, die für den Connector völlig unsichtbar sind.
- Die Regeln zur Volumenrundung sind Broker-spezifisch. Stellen Sie sicher, dass der konfigurierte `HedgeVolume` mit der Chargenstufe des Geräts kompatibel ist.
- Da die Strategie Marktaufträge sofort platziert, kann ein Ausrutschen in schnellen Märkten zu unvollständigen Absicherungen führen. Erhöhen Sie die Stop-Loss-Abstände, um dies bei Bedarf zu berücksichtigen.
