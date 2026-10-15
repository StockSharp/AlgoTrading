# ENewsLuckyw-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **ENewsLuckyw-Strategie** ist ein zeitbasiertes Breakout-System, das vom MetaTrader-Expertenberater *e-News-Lucky$* abgeleitet wurde. Zu einem geplanten Zeitpunkt übermittelt es Kauf-Stopp- und Verkaufs-Stopp-Aufträge rund um den aktuellen Preis, richtet sie kontinuierlich neu aus, während beide Aufträge aktiv sind, und führt ein Positionsmanagement durch, das die ursprüngliche MQL-Logik nachahmt. Schutzexits, optionales Trailing und eine Bereinigung am Tagesende runden den Workflow ab.

## Handelslogik
- **Geplante Straddle-Platzierung.** Bei `SetOrdersTime` storniert die Strategie alle verbleibenden ausstehenden Orders, misst den aktuellen Kerzenschluss und platziert symmetrische Stop-Orders bei `DistancePips` vom Marktpreis.
- **Kontinuierliche Auftragsaktualisierung.** Wenn beide ausstehenden Aufträge aktiv sind, werden sie bei jeder fertigen Kerze neu ausgerichtet, sodass der Straddle auf dem Preis zentriert bleibt, wie es der ursprüngliche Experte bei jedem neuen Balken getan hat.
- **Einstiegsvorbereitung.** Stop-Loss- und optionale Take-Profit-Level werden vorberechnet, sodass sie sofort bei Eröffnung einer Position angehängt werden können. Gegenüberliegende ausstehende Aufträge werden entfernt, sobald eine Position erscheint.
- **Trailing-Schutz.** Wenn `UseTrailing` aktiviert ist, verschiebt sich die Stop-Order um `TrailingStopPips`, wenn die Position um mindestens `TrailingStepPips` vorgerückt ist. Wenn `ProfitTrailing` aktiviert ist, beginnt das Trailing erst, wenn der Gewinn die Trailing-Distanz überschreitet, was den MQL-Schalter „ProfitTrailing“ repliziert.
- **Sitzungsbereinigung.** Am `DeleteOrdersTime` werden alle ausstehenden Aufträge storniert und alle offenen Positionen geschlossen, um zu vermeiden, dass das Risiko über Nacht gehalten wird.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Volume` | Ordervolumen in Lots, das für beide Stop-Orders verwendet wird. |
| `StopLossPips` | Schutzanschlagabstand. Null deaktiviert den Stopp. |
| `TakeProfitPips` | Optionale Take-Profit-Distanz. Null deaktiviert das Ziel. |
| `DistancePips` | Offset vom aktuellen Preis für die Breakout-Stop-Orders. |
| `UseTrailing` | Ermöglicht das Stoppen des Trailings, sobald die Position geöffnet ist. |
| `ProfitTrailing` | Erfordert, dass der nicht realisierte Gewinn die Trailing-Distanz überschreitet, bevor der Stop verschoben wird. |
| `TrailingStopPips` | Abstand zwischen Preis und Trailing Stop. |
| `TrailingStepPips` | Minimale Verbesserung erforderlich, bevor der Trailing Stop erneut aktualisiert wird. |
| `SetOrdersTime` | Tageszeit, zu der der Straddle platziert wird. |
| `DeleteOrdersTime` | Tageszeit für die Stornierung von Aufträgen und die Schließung von Positionen. |
| `CandleType` | Kerzenabonnement zur Zeiterfassung und Auftragsverwaltung. |

## Nutzungshinweise
1. Hängen Sie die Strategie an das gewünschte Instrument an und konfigurieren Sie `CandleType` so, dass es der Balkengröße entspricht, die Sie für die Wartung verwenden möchten (die Standardeinstellung sind 1-Minuten-Kerzen).
2. Legen Sie die Zeitplanparameter fest, um sie an Ihr Nachrichtenereignis oder Ihre Handelssitzung anzupassen.
3. Passen Sie Abstände und Risikokontrollen entsprechend der Instrumentenvolatilität an. Stellen Sie bei Forex-Symbolen sicher, dass der Preisschritt richtig konfiguriert ist, sodass `StopLossPips`, `TakeProfitPips` und `DistancePips` in die erwarteten Preisversätze übersetzt werden.
4. Das Trailing-System verwendet Stop- und Limit-Orders für Exits. Wenn Ihr Veranstaltungsort diese Ordertypen nicht unterstützt, ersetzen Sie sie durch Marktaustritte oder simulierte Orders, bevor Sie live gehen.
5. Die Strategie führt eine tägliche Zurücksetzung nach Datum durch. Wenn Sie es über Mitternacht in der Zeitzone der Börse ausführen, stellen Sie sicher, dass die Handelssitzung einen einzigen Handelstag umfasst.

## Konvertierungshinweise
- Die Strategie spiegelt den Arbeitsablauf des MQL-Experten wider: geplante Platzierung (`SetOrders`), stündliche Wartung (`ModifyOrders`), Entfernung widersprüchlicher ausstehender Bestellungen (`DeleteOppositeOrders`), nachgestellte Logik (`TrailingPositions`) und Bereinigung am Ende des Tages.
- Spread-bewusste Preisberechnungen aus dem MQL-Code werden anhand des letzten Kerzenschlusses angenähert, da StockSharp die Preise auf den `PriceStep` des Instruments normalisiert.
- Alle Ton-, Kontonummern- und Farbeinstellungen aus dem ursprünglichen Skript wurden weggelassen, da sie in StockSharps High-Level-API keine Entsprechung haben.
