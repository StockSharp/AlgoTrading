# HistoryInfoEaStrategy-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
**HistoryInfoEaStrategy** repliziert das MT4-Hilfsprogramm "HistoryInfo" auf StockSharp. Statt Text auf dem MetaTrader-Chart zu zeichnen, lauscht die Strategie dem `OnNewMyTrade`-Stream und aggregiert Statistiken für Trades, die einem gewählten Filter entsprechen. Die aggregierten Werte werden über die Eigenschaft `LastSnapshot` bereitgestellt und im Strategielog gespiegelt, sodass eine GUI oder ein Automatisierungsskript die Zusammenfassung in beliebiger Form anzeigen kann.

Die Strategie registriert niemals eigene Orders. Sie ist dafür gedacht, neben anderen automatisierten oder manuellen Strategien zu laufen, während diese Orders an den Broker senden. Jeder ausgeführte Trade, der den Filter erfüllt, trägt zu den Summen bei.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `FilterType` | Auswahlmodus, der bestimmt, wie Trades abgeglichen werden. Unterstützte Werte: `CountByUserOrderId`, `CountByComment`, `CountBySecurity`. |
| `MagicNumber` | Erwartete `Order.UserOrderId`. Wird nur verwendet, wenn `FilterType` gleich `CountByUserOrderId` ist. Leer lassen, um diesen Filter zu deaktivieren. |
| `OrderComment` | Präfix, das zu `Order.Comment` passen muss. Nur für den Modus `CountByComment` relevant. Der Standardwert (`\"OrdersComment\"`) imitiert den Platzhalter des MT4-Skripts und passt normalerweise zu keiner Order, bis er ersetzt wird. |
| `SecurityId` | Kennung des Instruments (`Security.Id`), die passen muss, wenn `FilterType` gleich `CountBySecurity` ist. Der Standard (`\"OrdersSymbol\"`) ist ein Platzhalter. |

## Aggregierte Kennzahlen
`LastSnapshot` wird nach jedem passenden Trade aktualisiert. Es enthält:

- `FirstTrade` / `LastTrade` - Zeitstempel des frühesten und spätesten verarbeiteten Trades.
- `TotalVolume` - kumuliertes ausgeführtes Volumen in den Volumeneinheiten des Trades (Lots, Kontrakte usw.).
- `TotalProfit` - Summe von `MyTrade.PnL` abzüglich gemeldeter Kommission, was den realisierten Gewinn in Kontowährung ergibt.
- `TotalPips` - Gewinn, umgerechnet in Pips mit `Security.PriceStep`, `Security.StepPrice` und MT4-ähnlicher Stellenbehandlung (5/3 Stellen multiplizieren den Punkt mit 10).
- `TradeCount` - Anzahl der Trades, die den Filter passiert haben.

Dieselbe Information wird in einer einzelnen Zeile ins Strategielog geschrieben und emuliert die MT4-`Comment()`-Ausgabe für schnelle Prüfung.

## Verwendung
1. Binden Sie die Strategie an dasselbe Portfolio und Wertpapier, das andere Strategien für Orderübermittlung verwenden.
2. Wählen Sie den gewünschten `FilterType` und füllen Sie den zugehörigen Parameter aus (Magic Number, Kommentarpräfix oder Wertpapierkennung).
3. Starten Sie die Strategie. Sobald der erste Trade ausgeführt wird, der den Kriterien entspricht, sind die Summen über `LastSnapshot` und das Log verfügbar.
4. Die Zähler werden bei jedem Strategieneustart oder manuellen Reset automatisch zurückgesetzt.

> **Hinweis:** Zur Berechnung von Pip-Summen benötigt die Strategie korrekte Instrumentenmetadaten. Stellen Sie sicher, dass `Security.PriceStep` und `Security.StepPrice` in der Board-Definition konfiguriert sind. Fehlt einer der Werte, bleibt der Pip-Zähler bei null, während der Gewinnwert weiter akkumuliert.

## Hinweise zur Umstellung
- Der MT4-Code iterierte auf jedem Tick über `OrdersHistoryTotal()`. In StockSharp reagiert die Strategie auf Echtzeit-`MyTrade`-Benachrichtigungen; es gibt also kein Polling und Berechnungen aktualisieren sich sofort bei einem Fill.
- MT4 speicherte Gewinn als `OrderProfit + OrderCommission + OrderSwap`. StockSharp liefert realisierten Gewinn über `MyTrade.PnL` und Kommission separat; Swap ist normalerweise bereits im PnL enthalten. Die Portierung zieht Kommission von `PnL` ab, um mit dem ursprünglichen Bericht konsistent zu bleiben.
- Die String-Platzhalter (`\"OrdersComment\"`, `\"OrdersSymbol\"`) bleiben erhalten, um den ursprünglichen Defaults zu ähneln. Ersetzen Sie sie vor dem Start durch tatsächliche Werte, wenn Sie Treffer erwarten.
- Visuelle Chart-Ausgabe aus MT4 wird durch strukturierte Daten (`LastSnapshot`) und Logzeilen ersetzt, damit Integratoren selbst entscheiden können, wie die Information dargestellt wird.
- Die Strategie vermeidet absichtlich das Erstellen neuer Orders und kann daher im Read-only-Modus gestartet werden, um fremde Trade-Streams zu analysieren, ohne einzugreifen.

## Erweiterungsideen
- Abonnieren Sie `LastSnapshot`-Aktualisierungen und leiten Sie die Information an ein Dashboard oder einen Telemetrie-Collector weiter.
- Erweitern Sie die Klasse um zusätzliche Filter (zum Beispiel nach Portfolio oder benutzerdefinierten Strategie-Tags), wenn der Connector die relevanten Metadaten bereitstellt.
- Kombinieren Sie die Strategie mit einem periodischen Timer, um historische Zusammenfassungen in einen CSV/JSON-Bericht zu exportieren.
