# OpenTiks-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die OpenTiks-Strategie portiert den klassischen MetaTrader-Expertenberater `OpenTiks.mq4` in das StockSharp-Ökosystem. Der ursprüngliche Roboter
suchte nach einer Kerzentreppe mit streng monotonen Höhen und Öffnungen, um frühe Ausbrüche zu erkennen. Sobald ein Signal auftauchte, war es
Eröffnete eine Marktorder, fügte optional einen Schutzstopp hinzu und verlangsamte dann die Position, während er nach und nach Gewinne mitnahm
durch wiederholtes Halbieren der Belichtung. Die StockSharp-Version spiegelt diese Ideen mit hochrangigen API-Anrufen, Kerzenabonnements,
und die integrierten Ordnungshelfer, damit die Logik in Designer, Runner oder einer beliebigen benutzerdefinierten S#-Anwendung ausgeführt wird.

## Mustererkennung
Ein Handel kann gestartet werden, wenn **vier aufeinanderfolgende Kerzen** einem der folgenden Muster entsprechen:

- **Aufwärtstrend** – für die aktuelle Kerze und die vorherigen drei Balken: Jeder `High` ist strikt höher als der vorherige
`High`, und jedes `Open` ist strikt höher als das vorhergehende `Open`.
- **Bärischer Ausbruch** – für dasselbe Vier-Balken-Fenster: Jeder `High` ist streng niedriger als der vorherige `High` und jeder `Open`
ist strikt niedriger als der vorherige `Open`.

Signale werden auf abgeschlossene Kerzen ausgewertet, die vom konfigurierten `CandleType` geliefert werden. Wenn die Ausbruchsbedingung erfüllt ist
Die Strategie sendet eine Marktorder mit dem konfigurierten Volumen (normalisiert auf `VolumeStep` des Wertpapiers und begrenzt durch `MinVolume`
und `MaxVolume`). Der Parameter `MaxOrders` begrenzt, wie viele gleichzeitige Einträge vorhanden sein können; ein Wert von Null deaktiviert die Prüfung,
während jede positive Zahl neue Geschäfte blockiert, sobald die absolute Nettoposition dividiert durch das normalisierte Auftragsvolumen diesen Wert erreicht
Grenze.

## Risiko- und Exit-Management
- **Stop-Loss** – wenn `StopLossPoints` größer als Null ist, überwacht die Strategie die letzte Kerze auf Preisumkehrungen. Lange
Positionen werden liquidiert, wenn das Tief der Kerze `entryPrice - StopLossPoints × PriceStep` durchdringt. Short-Positionen werden beendet, wenn
das High berührt `entryPrice + StopLossPoints × PriceStep`.
- **Trailing Stop** – sobald der Preis um mindestens `TrailingStopPoints × PriceStep` über den Einstieg hinaus steigt, wird ein Trailing Stop aktiviert
im gleichen Abstand hinter (bei Longs) oder vor (bei Shorts) des Schlusskurses. Jedes Mal, wenn sich das nachlaufende Niveau verbessert, wird die
Restposition wird optional reduziert.
- **Progressive Gewinnmitnahme** – wenn `UsePartialClose` aktiviert ist, schließt die Strategie jedes Mal die Hälfte des aktuellen Engagements
Der Trailing Stop bewegt sich vorwärts. Die Volumina werden auf `VolumeStep` des Instruments gerundet. Wenn die halbierte Größe unterschritten wird
`MinVolume`, stattdessen wird die gesamte Position geschlossen, was dem Verhalten des MetaTrader-Experten entspricht.

Alle Stop- und Trailing-Berechnungen werden für beendete Kerzen durchgeführt, sodass Ausstiege beim nächsten Balkenschluss erfolgen und nicht bei jedem
eingehender Tick. Dadurch bleibt die Implementierung mit dem High-Level-API von StockSharp konsistent und bleibt gleichzeitig nah am Original
Idee, auf neue Bars zu reagieren.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `OrderVolume` | `decimal` | `0.1` | Basislosgröße für jeden Markteintritt. Die Strategie normalisiert es auf den Volumenschritt und die Limits des Wertpapiers. |
| `StopLossPoints` | `decimal` | `0` | Schutzstoppabstand, ausgedrückt in Preispunkten (Preisschritten). Ein Wert von Null deaktiviert den Stopp. |
| `TrailingStopPoints` | `decimal` | `30` | Abstand, der durch den Trailing Stop aufrechterhalten wird, sobald die Position in die Gewinnzone geht, auch in Preispunkten. |
| `MaxOrders` | `int` | `1` | Maximale Anzahl gleichzeitig geöffneter Einträge. Null hebt die Einschränkung auf. |
| `UsePartialClose` | `bool` | `true` | Aktiviert die Halbierungslogik, die Gewinne festlegt, wenn der Trailing-Stop vorrückt. |
| `CandleType` | `DataType` | `1 minute` Zeitrahmen | Primäres Kerzenabonnement, das für die Signalauswertung und Trailing-Prüfungen verwendet wird. |

## Hinweise zur Implementierung
- StockSharp arbeitet mit **Nettopositionen**, sodass alle Orders für das konfigurierte Wertpapier zu einem einzigen Long- oder Short-Order zusammengefasst werden
Belichtung. Der Parameter `MaxOrders` wirkt sich daher auf die aggregierte Position und nicht auf einzelne MetaTrader-Tickets aus.
- Candle-basiertes Trailing bedeutet, dass Stop-Checks einmal pro abgeschlossenem Balken erfolgen. Händler, die Schutz auf Tick-Ebene benötigen, können dies reduzieren
Kerzengröße oder erweitern Sie die Logik, um Trades zu abonnieren.
- Bei Teilschließungen werden die Metadaten des Instruments (`VolumeStep`, `MinVolume`, `MaxVolume`) berücksichtigt, um abgelehnte Bestellungen zu vermeiden.
- Inline-Kommentare auf Englisch heben die wichtigsten Entscheidungspunkte hervor, sodass die Datei gleichzeitig als Lehrmaterial für die Umsetzung der Idee dient
zu anderen Breakout- oder Money-Management-Experimenten.

## Anwendungstipps
1. Wählen Sie einen Kerzentyp aus, der dem im ursprünglichen MetaTrader-Setup verwendeten Zeitrahmen entspricht (z. B. M1 oder M5).
2. Überprüfen Sie die Schritt- und Chargeneinstellungen des Instruments. Die Standardeinstellung `OrderVolume` von `0.1` eignet sich für Verträge im Forex-Stil, kann es aber sein
angepasst an Futures, Aktien oder Kryptosymbole.
3. Experimentieren Sie mit `TrailingStopPoints` und `UsePartialClose`, um ein Gleichgewicht zwischen aggressiver Gewinnbindung und -vermietung zu finden
Siegerlauf.
4. Kombinieren Sie die Strategie mit StockSharp-Diagrammen, um das Treppenmuster visuell zu bestätigen und die Teilausstiege in der Realität zu beobachten
Zeit.
