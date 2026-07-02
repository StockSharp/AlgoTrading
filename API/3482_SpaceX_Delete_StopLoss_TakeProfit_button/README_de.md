# SpaceX StopLoss TakeProfit Button-Strategie löschen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert die Schaltfläche **"DELETE SL_TP"** aus dem ursprünglichen MetaTrader-Panel *SpaceX_Delete_StopLoss_TakeProfit_button.mq5*. Es ist als Hilfsmittel konzipiert, das das aktuelle Portfolio scannt und alle aktiven schützenden Stop-Loss- oder Take-Profit-Orders storniert, die zu offenen Positionen gehören. Die Konvertierung zielt auf die übergeordnete Ebene StockSharp API ab und bietet eine bequeme Möglichkeit, Schutzklammern zu entfernen, ohne jedes Ticket manuell öffnen zu müssen.

Die Strategie eröffnet oder schließt keine Positionen selbstständig. Es überprüft lediglich die bereits offenen Geschäfte und entfernt ihre Schutzanweisungen, wenn es dazu aufgefordert wird. Dadurch eignet es sich für Händler, die Positionen manuell oder über andere automatisierte Systeme verwalten, aber einen schnellen Panikknopf wünschen, der alle Stop- und Take-Profit-Orders löscht.

## Ursprünglicher Expert Advisor
Die MetaTrader-Version zeichnet ein einzelnes Dialogfenster mit einer **DELETE SL_TP**-Schaltfläche. Immer wenn der Knopf gedrückt wird, durchläuft der Experte alle offenen Positionen und ruft `PositionModify` mit Nullwerten für Stop-Loss und Take-Profit auf. Dadurch wird jede Schutzebene von der Position gelöst, während das Positionsvolumen unangetastet bleibt.

Wichtige Verhaltensweisen des Quellcodes:

* Es werden keine Markteintritte oder -austritte vorgenommen.
* Alle Symbole im Terminal werden ohne Filterung verarbeitet.
* Es werden nur Stop-Loss- und Take-Profit-Werte entfernt; Bestellkommentare und magische Zahlen bleiben erhalten.
* Die Aktion wird ausschließlich über den GUI-Button ausgelöst.

## StockSharp Implementierung
Durch die StockSharp-Konvertierung konzentriert sich das Verhalten weiterhin auf die Entfernung von Schutzanordnungen. Anstelle eines GUI-Dialogs wird die Aktion durch Strategieparameter gesteuert, die über die StockSharp-Benutzeroberfläche oder über den Code umgeschaltet werden können. Die Strategie funktioniert mit jedem Broker-Adapter, der Order-Stop- oder Take-Profit-Informationen offenlegt.

Es werden zwei Ausführungsmodi unterstützt:

1. **Automatische Ausführung beim Start** – optional. Wenn die Strategie aktiviert ist, werden Schutzbefehle sofort nach Beginn der Ausführung entfernt.
2. **Manueller Befehl** – ein boolescher Parameter, der die ursprüngliche Schaltfläche nachahmt. Wenn Sie den Parameter auf `true` setzen, wird beim nächsten Timer-Tick eine Bereinigung geplant, nach der das Flag auf `false` zurückgesetzt wird.

Die Konvertierung storniert Schutzaufträge, indem `CancelOrder` für jeden aktiven Auftrag aufgerufen wird, der als Stop-Loss, Take-Profit oder ein anderer bedingter Schutzauftrag identifiziert wird. Positionsvolumina werden niemals berührt.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| **Beim Start ausführen** (`ApplyOnStart`) | Bei `true` entfernt die Strategie Schutzanordnungen unmittelbar nach Beginn der Strategie. | `true` |
| **Alle Wertpapiere** (`AffectAllSecurities`) | Verarbeitet alle Portfoliopositionen. Bei `false` wird nur die Strategiesicherheit berücksichtigt. | `true` |
| **Anfrage löschen** (`DeleteRequest`) | Manueller Auslöser, der die Schaltfläche MetaTrader emuliert. Drehen Sie es auf `true`, um eine einmalige Entfernung durchzuführen. es wird automatisch zurückgesetzt. | `false` |
| **Abfrageintervall(e)** (`PollingIntervalSeconds`) | Timer-Intervall in Sekunden, das zum Abfragen des manuellen Triggers verwendet wird. Der Timer führt die Löschanforderung auch aus, wenn `Run On Start` deaktiviert ist. | `1` |

## Wie es funktioniert
1. Beim Start validiert die Strategie das Abfrageintervall und startet einen Timer, der alle *N* Sekunden aktiviert wird.
2. Wenn **Beim Start ausführen** aktiviert ist, wird eine sofortige Bereinigung ausgeführt.
3. Bei jedem Timer-Tick wird das Flag **Delete Request** überprüft. Wenn das Flag `true` ist, sammelt die Strategie die Wertpapiere, die offene Positionen innerhalb des konfigurierten Bereichs haben, und storniert alle Schutzaufträge für diese Instrumente.
4. Nach der Ausführung wird das manuelle Flag auf `false` zurückgesetzt, um sicherzustellen, dass die Aktion nur einmal pro Anfrage ausgeführt wird.

### Identifizierung von Schutzanordnungen
Eine Anordnung wird als Schutzanordnung behandelt, wenn eine der folgenden Bedingungen erfüllt ist:

* Der Bestelltyp ist `Stop`, `TakeProfit` oder `Conditional`.
* Es liegt ein Stop-Preis, ein Take-Profit-Preis oder eine Orderbedingung ungleich Null vor.

Diese konservative Definition deckt die gängigsten Adapter ab. Wenn ein Konnektor benutzerdefinierte Auftragstypen oder Bedingungen für die Stoppverwaltung verwendet, erweitern Sie die Erkennungslogik entsprechend.

## Nutzungstipps
* Hängen Sie die Strategie an den Connector an, der Ihre offenen Trades verwaltet. Stellen Sie sicher, dass alle Positionen, die Sie verwalten möchten, für das konfigurierte Portfolio sichtbar sind.
* Lösen Sie die Löschanforderung aus dem Parameterraster in Hydra oder Terminal aus, indem Sie das Kontrollkästchen **Anfrage löschen** aktivieren.
* Kombinieren Sie das Dienstprogramm mit anderen Strategien, um Schutzklammern vorübergehend zu entfernen, bevor Sie neue anbringen.
* Halten Sie das Abfrageintervall klein (standardmäßig 1 Sekunde), um ein reaktionsfähiges Tastenerlebnis zu gewährleisten. Erhöhen Sie den Wert, wenn Sie die Timer-Aktivität reduzieren möchten.

## Unterschiede zum Original EA
* Die Schaltfläche MetaTrader wirkt sofort über einen Diagrammdialog. In StockSharp wird die Aktion als Parameter angezeigt, der von einem Timer überwacht wird.
* Schutzbefehle werden aufgehoben, anstatt Positionsobjekte zu ändern. Dies ist der natürliche Ansatz innerhalb von StockSharp, da Stop-Loss- und Take-Profit-Level als separate Orders und nicht als Inline-Positionseigenschaften dargestellt werden.
* Die optionale Scope-Steuerung ermöglicht die Beschränkung des Vorgangs auf die angeschlossene Sicherheit, was im Vergleich zum Original-Experten einen zusätzlichen Komfort darstellt.

## Einschränkungen
* Die Strategie erfordert, dass der Adapter Stop-Loss- und Take-Profit-Orders als aktive Orders offenlegt. Wenn der Broker serverseitige Schutzstufen verwendet, die nicht als Aufträge dargestellt werden, ist eine Stornierung dieser möglicherweise nicht möglich.
* Es wird kein GUI-Dialog erstellt. Die Steuerung erfolgt vollständig über Strategieparameter oder programmatischen Zugriff.
* Das Dienstprogramm erstellt keine Schutzstufen neu. es entfernt sie nur.

## Testen
Die Strategie umfasst keine dedizierten automatisierten Tests, da sie Hilfsfunktionen ohne komplexe Berechnungen ausführt. Manuelle Tests können durchgeführt werden, indem Beispielpositionen geöffnet, die Strategie angehängt und überprüft werden, dass alle Schutzanweisungen nach jedem Auslöser storniert werden.
