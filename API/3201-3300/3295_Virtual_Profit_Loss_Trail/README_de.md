# Virtual-Profit/Loss-Trail-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

`VirtualProfitLossTrailStrategy` reproduziert das Verhalten des MetaTrader Expert Advisors "Virtual Profit Loss Trail" in StockSharp. Die Strategie eröffnet selbst niemals neue Positionen. Stattdessen überwacht sie kontinuierlich die aktuelle Position des ausgewählten Wertpapiers und wendet Schutzlogik an:

- Eine konfigurierbare Take-Profit-Distanz in Pips.
- Eine konfigurierbare Stop-Loss-Distanz in Pips.
- Einen virtuellen Trailing Stop, der nach Erreichen eines Mindestgewinns aktiviert wird und nur dann mit dem Markt gleitet, wenn der Preis um den angegebenen Trailing-Schritt vorankommt.

Da die Schutzlevel virtuell sind, werden keine tatsächlichen Stop- oder Limit-Orders an die Börse gesendet. Die Strategie überwacht beste Bid-/Ask-Aktualisierungen und schließt die offene Position mit einer Marktorder, wenn eines der virtuellen Niveaus berührt wird.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| **Take-profit (pips)** | Distanz zwischen Einstiegspreis und Gewinnziel. Auf `0` setzen, um den Take-Profit-Ausstieg zu deaktivieren. |
| **Stop-loss (pips)** | Distanz zwischen Einstiegspreis und Schutz-Stop. Auf `0` setzen, um den Stop-Loss-Ausstieg zu deaktivieren. |
| **Trailing stop (pips)** | Distanz zur Berechnung des Trailing Stops. Bei `0` ist die Trailing-Logik vollständig deaktiviert. |
| **Trailing step (pips)** | Zusätzlicher Gewinn, der erzielt werden muss, bevor der Trailing Stop weiter verschoben wird. `0` verwenden, um den Trail bei jedem neuen Hoch/Tief zu verschieben. |
| **Trailing activation (pips)** | Mindestgewinn, der gesichert werden muss, bevor der Trailing Stop aktiv wird. Bei `0` startet Trailing sofort nach dem Positionseinstieg. |

Alle Distanzen werden in Pip-Einheiten gemessen. Die Strategie leitet die Pip-Größe automatisch aus dem Preisschritt des Wertpapiers ab: Bei Symbolen mit drei oder fünf Dezimalstellen ist ein Pip als zehn Preisschritte definiert, ansonsten als ein Schritt.

## Logik

1. **Marktdatensubscription** - die Strategie abonniert Level1-Daten, um beste Bid- und Ask-Aktualisierungen zu erhalten. Nur fertige Aktualisierungen werden verarbeitet, sodass der Algorithmus sowohl in Echtzeit als auch bei historischen Replays funktioniert.
2. **Long-Positionsverwaltung** - wenn die Nettoposition long ist, berechnet die Strategie virtuelle Stop-Loss-, Take-Profit- und Trailing-Stop-Niveaus auf Basis des durchschnittlichen Einstiegspreises. Berührt der beste Bid Stop-Loss oder Take-Profit, wird die Position sofort geschlossen. Sobald der Aktivierungsgewinn erreicht ist, folgt der Trailing Stop dem Preis nach oben. Der Stop wird nur weitergezogen, wenn die Trailing-Schritt-Anforderung erfüllt ist.
3. **Short-Positionsverwaltung** - dieselbe Logik wird symmetrisch mit dem besten Ask für Ausstiege aus Short-Positionen angewendet.
4. **Reset-Verhalten** - wenn die Position vollständig geschlossen ist, werden interne Trailing-Referenzen zurückgesetzt, um versehentliche Wiedereinstiegssignale zu verhindern.

## Nutzungstipps

- Binden Sie die Strategie an einen Connector und ein Wertpapier, das bereits eine offene Position hat oder Orders von anderen Strategien oder manuellem Handel erhalten wird. Der Manager steuert die aggregierte Positionsgröße.
- Stellen Sie sicher, dass Level1-Daten verfügbar sind; ohne aktuelle Bid-/Ask-Werte können die virtuellen Niveaus nicht bewertet werden.
- Die Strategie kann mit jeder einstiegserzeugenden Strategie kombiniert werden, indem beide unter demselben Portfolio und Wertpapier laufen. Nur eine Instanz sollte die Schutzlogik verwalten, um Konflikte zu vermeiden.

## Unterschiede zum MQL-Expert

- Die StockSharp-Version arbeitet mit aggregierten Positionen statt mit einzelnen Ordertickets. Sie berechnet automatisch den von der Plattform bereitgestellten durchschnittlichen Einstiegspreis.
- Visuelles Linienzeichnen und Tonalarme des ursprünglichen Experts werden durch Logging in StockSharp ersetzt. Schutzaktionen sind im Strategiejournal sichtbar.
- Die gleiche pipbasierte Konfiguration bleibt erhalten, einschließlich Trailing-Aktivierungsschwelle und inkrementellem Trailing-Schritt.

## Dateien

- `CS/VirtualProfitLossTrailStrategy.cs` - C#-Implementierung der Strategie.
- `README.md` - diese Dokumentation.
- `README_zh.md` - Übersetzung ins vereinfachte Chinesisch.
- `README_ru.md` - russische Übersetzung.
