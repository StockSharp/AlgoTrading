# Zufällige Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader 5 Expertenberaters „Zufällig“ (MQL ID 39835). Der ursprüngliche Bot demonstriert, wie sich ein rein zufälliger Entscheidungsprozess verhält, wenn er gezwungen ist, immer auf dem Markt zu sein. Jeder abgeschlossene Balken löst einen Münzwurf aus, der bestimmt, ob die nächste Aktion ein Kauf oder ein Verkauf ist. Die StockSharp-Version behält die gleiche Idee bei, drückt sie jedoch mit API-Primitiven auf hoher Ebene (`SubscribeCandles`, `BuyMarket`, `SellMarket`) aus und lässt sich reibungslos in Designer oder Runner integrieren.

Die Implementierung vermeidet bewusst Take-Profit, Stop-Loss oder Trailing Stops und spiegelt das Referenzskript MQL wider. Es dient daher eher als Testgerät oder pädagogisches Beispiel denn als gewinnbringende Strategie.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie (`CandleType`). Das Standardintervall beträgt 15 Minuten, um das Verhalten „aktueller Zeitrahmen“ von MetaTrader nachzuahmen.
2. Sobald eine Kerze fertig ist, prüfen Sie, ob ein vorheriger Trade geschlossen werden muss. Wenn `CloseBeforeReversal` aktiviert ist, reduziert die Strategie die Position und wartet auf die Bestätigung, dass kein Risiko mehr vorhanden ist, bevor die nächste Order erteilt wird.
3. Erzeugen Sie eine zufällige Richtung mithilfe eines Pseudozufallszahlengenerators. Der optionale Parameter `RandomSeed` ermöglicht deterministische Sequenzen für reproduzierbare Backtests.
4. Senden Sie eine Marktorder mit dem festen `TradeVolume`. Long- und Short-Trades sind symmetrisch und es gibt keine Schutzaufträge. Die Protokollierung kann über `LogSignals` aktiviert werden, um jede zufällige Entscheidung zu verfolgen.

Da jede Kerze nur eine zufällige Entscheidung auslöst, ist die Strategie entweder flach oder hält zu jedem Zeitpunkt eine einzelne Position. Positionen werden erst umgekehrt oder geschlossen, wenn der nächste Balken erscheint.

## Auftragsmanagement und Risiko
- Alle Ein- und Ausgänge werden mit `BuyMarket` / `SellMarket` unter Verwendung des konfigurierten Volumes durchgeführt. Es gibt keine Limit- oder Stop-Orders.
- Wenn `CloseBeforeReversal` deaktiviert ist, kann die Strategie Positionen hintereinander halten: Ein neues Zufallssignal kann die Gegenseite sofort öffnen, ohne zuerst den vorherigen Trade explizit zu schließen.
- Es ist keine Geldverwaltung oder Kontosicherung implementiert. Der Zweck des Ports besteht darin, das Verhalten des Referenz-Expertenberaters für Bildungs- und Infrastrukturtestszenarien zu reproduzieren.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Basisauftragsgröße, die für jeden Zufallseintrag verwendet wird. Muss positiv bleiben. |
| `CloseBeforeReversal` | Zwingt die Strategie, die aktuelle Position zu schließen, bevor der nächste zufällige Trade durchgeführt wird. |
| `LogSignals` | Schreibt `AddInfoLog` Nachrichten, wann immer eine zufällige Richtung generiert wird. |
| `CandleType` | Zeitrahmen, der die Kerzenserie erzeugt, die den zufälligen Münzwurf antreibt. |
| `RandomSeed` | Startwert für den Pseudozufallszahlengenerator. Verwenden Sie `0`, um sich auf die Systemuhr zu verlassen. |

## Nutzungshinweise
- Der Port behält das Fehlen von Take-Profit- und Stop-Loss-Levels bei, genau wie die Referenz MQL. Eventuelle Risikokontrollen müssen manuell hinzugefügt werden, wenn die Strategie für Experimente mit echtem Kapital verwendet wird.
- Deterministische Seeds sind nützlich, um reproduzierbare Datensätze bei der Optimierung oder dem Benchmarking von zufälligem Verhalten zu erstellen.
- Bei Tests wird empfohlen, die Protokollierung zu aktivieren, da eine reine Zufallsstrategie kaum visuelles Feedback zum Diagramm bietet.
