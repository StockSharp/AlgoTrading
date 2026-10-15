# Unterbrechen Sie den Handel mit der Strategie für aufeinanderfolgende Verluste
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Pause Trading On Consecutive Loss-Strategie** reproduziert die Risikokontrolllogik des MetaTrader 4-Expertenberaters *"Pause Trading On Consecutive Loss"*. Das ursprüngliche Skript überwachte die letzten abgeschlossenen Geschäfte, zählte, wie viele davon mit einem negativen Gewinn endeten, und setzte neue Aufträge aus, wenn die Verluststrähne innerhalb eines kurzen Zeitfensters ein benutzerdefiniertes Limit überschritt. Der StockSharp-Port behält dieses Verhalten bei und umschließt gleichzeitig ein Einstiegsmodell mit minimalem Momentum, sodass der Pausenmechanismus innerhalb der eigenständigen Strategie ausgewertet werden kann.

## Wie es funktioniert

1. Die Strategie abonniert Zeitrahmenkerzen, die durch `CandleType` angegeben werden. Immer wenn eine fertige Kerze eintrifft, wird der Schlusskurs mit dem vorherigen Schlusskurs verglichen. Bei einem Anstieg versucht die Strategie einen Long-Einstieg; ist er gesunken, wird ein Short-Einstieg in Betracht gezogen. Positionen werden immer dann geschlossen, wenn eine bullische Position einer bärischen Kerze gegenübersteht (Schluss unter der Eröffnung) oder eine bärische Position einer zinsbullischen Kerze gegenübersteht (Schluss über der Eröffnung).
2. Nach jeder geschlossenen Position wird der realisierte Gewinn der Strategie überprüft. Verliererergebnisse stellen ihren Abschlusszeitstempel in eine interne FIFO-Liste, die nur aufeinanderfolgende Verluste speichert. Gewinnbringende oder ausgeglichene Exits löschen die Liste, genau wie die MQL-Schleife abgebrochen wurde, als sie auf ein Geschäft ohne Verluste stieß.
3. Wenn die Liste `ConsecutiveLosses` Elemente erreicht, prüft die Strategie, ob der Zeitunterschied zwischen dem ältesten und dem neuesten Verlust innerhalb von `WithinMinutes` liegt. Wenn dies der Fall ist, wird der Handel unterbrochen, bis `PauseMinutes` seit dem letzten Handelsschluss verstrichen ist. Während der Pause werden keine neuen Marktaufträge übermittelt, aber das bestehende Positionsmanagement läuft weiter, sodass sich das Buch auf natürliche Weise verflachen kann.
4. Sobald die Pause abgelaufen ist, wird die Liste der Verluste gelöscht und der Handel wird automatisch fortgesetzt. Das Verhalten ahmt die ursprünglichen Funktionen `CheckLastNLossDifference` und `lastOrderCloseTime` nach, ohne auf einen dauerhaften Scan des Bestellverlaufs angewiesen zu sein.

Die Implementierung nutzt die High-Level-Kerzenabonnements (`SubscribeCandles`) von StockSharp und den integrierten PnL-Manager, um realisierte Gewinne zu überwachen. Eine einfache Warteschlange (`Queue<DateTimeOffset>`) erfasst die Zeitstempel der Verlustserie und respektiert dabei das Verbot der redundanten manuellen Verlaufsdurchquerung.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | Zeitrahmen von 5 Minuten | Kerzenaggregation, die für die einfachen Momentum-Einträge verwendet wird. |
| `OrderVolume` | `0.1` | Volumen (in Lots/Kontrakten), das mit jedem Ein- und Ausstiegsauftrag gesendet wird. |
| `ConsecutiveLosses` | `3` | Anzahl der aufeinanderfolgenden Verlustpositionen, die erforderlich sind, bevor neue Trades pausiert werden. |
| `WithinMinutes` | `20` | Maximal zulässige Anzahl von Minuten zwischen der ersten und der letzten Niederlage im Streak. Ein Wert von Null deaktiviert die Fensterprüfung. |
| `PauseMinutes` | `20` | Dauer der Handelsaussetzung nach Feststellung der Verlustserie. |

## Notizen

- Die Warteschlange der Verlustzeitstempel wird nur dann gefüllt, wenn die Strategie flach ist und gerade einen Verlust realisiert hat. Teilabschlüsse oder profitable Trades verlängern den Streak nicht und verhindern so Fehlalarme.
- Der Pausentimer wird für jede fertige Kerze ausgewertet. Wenn `PauseMinutes` vergehen, während die Strategie inaktiv ist, wird der Handel mit der nächsten Kerze sofort freigeschaltet.
- Da die StockSharp-Version auf einer Netting-Position arbeitet, wird die realisierte PnL-Differenz aus `PnLManager.RealizedPnL` abgeleitet, wodurch die MetaTrader-Verlaufssuche getreu widergespiegelt wird, ohne dass das gesamte Auftragsprotokoll erneut verarbeitet werden muss.
