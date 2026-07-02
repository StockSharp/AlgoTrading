# Strategie zur Risikokontrolle bei Abschlussaufträgen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Close Orders Strategy** ist ein Risikomanagement-Dienstprogramm, das das Verhalten des ursprünglichen MQL-Expertenberaters *CloseOrders.mq4* widerspiegelt. Es überwacht kontinuierlich die schwankenden Gewinne und Verluste offener Positionen und löst entsprechende Aufträge automatisch auf, sobald entweder das Gewinnziel oder die Cut-Loss-Schwelle erreicht ist. Dadurch eignet es sich zum Schutz eines Portfolios oder zur Synchronisierung von Exits über mehrere Strategien hinweg.

## Wie es funktioniert
1. Die Strategie abonniert eine konfigurierbare Kerzenserie (standardmäßig 1 Minute) und wertet den aktuellen schwebenden PnL aus, wenn eine Kerze schließt.
2. Der variable PnL wird für die aktiven Portfoliopositionen berechnet. Wenn eine magische Zahl angegeben wird, werden nur Positionen einbezogen, deren interner `StrategyId` mit dem konfigurierten Wert übereinstimmt.
3. Wenn der variable Gewinn gleich oder größer als der Zielbetrag ist, werden alle passenden Aufträge und Positionen geschlossen.
4. Wenn der variable Gewinn unter den konfigurierten Cut-Loss (eine negative Zahl) fällt, wird die gleiche Liquidationsroutine ausgelöst, um weitere Verluste zu minimieren.
5. Aktive Aufträge, die den Magic-Number-Filter erfüllen, werden vor dem Abflachen der Positionen storniert, um sicherzustellen, dass während der Liquidation kein neues Risiko eröffnet wird.

Die Liquidationsroutine läuft weiter, bis alle übereinstimmenden Positionen leer sind, um sicherzustellen, dass Teilbesetzungen ordnungsgemäß gehandhabt werden.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| **Zielgewinngeld** | Variabler Gewinn (in Kontowährung), der die Liquidation passender Aufträge auslöst. Muss größer als Null sein. |
| **Verlustgelder reduzieren** | Negativer variabler PnL (in Kontowährung), der die Liquidation erzwingt. Ein Wert von `0` deaktiviert den verlustbasierten Exit. |
| **Magische Zahl** | Optionaler Strategiebezeichner. Lassen Sie das Feld leer, um alle offenen Positionen zu verwalten. Andernfalls sind nur Positionen betroffen, deren `StrategyId` dem angegebenen Wert entspricht. |
| **Kerzentyp** | Kerzenserien, die dazu dienen, regelmäßige Gewinnüberprüfungen auszulösen. Passen Sie den Zeitrahmen an, wenn eine Überwachung mit höherer Frequenz erforderlich ist. |

## Hinweise zur Implementierung
- Das magische Zahlenkonzept MQL wird den Feldern `UserOrderId`/`StrategyId` in StockSharp zugeordnet. Stellen Sie sicher, dass die zu verwaltenden Strategien dieselbe Kennung verwenden.
- Zur Einrückung werden Tabulatoren verwendet und die Datei folgt der gemeinsamen Struktur, die für konvertierte Strategien erforderlich ist.
- Die Strategie storniert ausstehende Aufträge vor dem Senden von Marktaufträgen, um das Risiko einzudämmen und einen sofortigen Wiedereintritt zu verhindern.
- Ein Startschutz kann hinzugefügt werden, wenn die Strategie mit Live-Handelskomponenten kombiniert wird, die im Notfall behandelt werden müssen.

## Anwendungstipps
- Setzen Sie die Strategie zusammen mit Handelsstrategien ein, die einen benutzerdefinierten `StrategyId` festlegen, um die Exit-Logik zu zentralisieren.
- Passen Sie den Parameter `Candle Type` an, um Reaktionsfähigkeit und Ressourcennutzung in Einklang zu bringen. Kürzere Zeitrahmen ermöglichen eine schnellere Reaktion auf PnL-Änderungen.
- Kombinieren Sie das Dienstprogramm mit Benachrichtigungen, um Benachrichtigungen zu erhalten, wenn eine automatische Liquidation durchgeführt wird.
