# MACD Signal ATR Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **MACD-Signalstrategie** portiert den MetaTrader-Experten `MACD_signal.mq4` nach StockSharp. Der ursprüngliche Roboter hat die gemessen
MACD-Histogramm gegen ein ATR-basiertes Volatilitätsband und eröffnete eine einzelne Marktorder, wann immer das Histogramm dieses kreuzte
Band. Diese C#-Version erstellt die gleiche Momentum-Breakout-Logik unter Verwendung des High-Level-API von StockSharp neu und speichert die vorherige
Histogramm und ATR-Messwerte explizit und dokumentiert jede Geldverwaltungsregel mit benannten Parametern und Englisch
Kommentare im Quellcode.

Im Gegensatz zur MetaTrader-Implementierung, die Tickets direkt änderte, arbeitet der StockSharp-Port mit Nettopositionen. Es
Daher wird die aktuelle Belichtung geschlossen, bevor die Richtung geändert wird, und die Trailing Stops werden intern aktualisiert, anstatt sich darauf zu verlassen
Brokerseitige `OrderModify`-Aufrufe.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie (`CandleType`) und verarbeiten Sie **nur** fertige Kerzen, um Teilbalken zu vermeiden
Lärm.
2. Füttern Sie einen `MovingAverageConvergenceDivergenceSignal`-Indikator mit den ausgewählten Schnell-, Langsam- und Signallängen von EMA. Die
Der Histogrammwert (`MACD - signal`) wird jedes Mal gespeichert, wenn ein Balken schließt.
3. Berechnen Sie den `AverageTrueRange` für dieselben Kerzen. Der Wert aus dem **vorherigen** Balken wird mit multipliziert
`ThresholdMultiplier`, um den Schwellenwert `rr = ATR * LEVEL` von MQL neu zu erstellen.
4. Erkennen Sie einen zinsbullischen Ausbruch, wenn das aktuelle Histogramm `+threshold` überschreitet, während das vorherige Histogramm noch darunter lag
es. Wenn das Konto ein Flat-Konto ist oder Short- und Long-Handel bis `Direction` zulässig ist, senden Sie eine Marktkauforder mit der Größe von
`TradeVolume`.
5. Erkennen Sie einen rückläufigen Ausbruch, wenn das Histogramm unter `-threshold` fällt, nachdem es bei der vorherigen Kerze darüber lag. Wenn
Die Strategie ist flach oder der Long- und Short-Handel ist aktiviert. Erteilen Sie einen Marktverkaufsauftrag mit der Größe `TradeVolume`.
6. Offene Positionen in jedem Takt verwalten:
   - Long-Positionen schließen, sobald das Histogramm negativ wird; Kurzschlüsse schließen, wenn es positiv wird;
   - Überwachen Sie die feste Take-Profit-Distanz (`TakeProfitPoints`) im Vergleich zu Kerzenhochs oder -tiefs, um das Original zu emulieren
MetaTrader Take-Profit-Parameter;
   - Aktualisieren Sie die Trailing Stops, sobald sich der Preis mehr als `TrailingStopPoints` vom Einstieg entfernt, und beenden Sie den Kurs, wenn die Kerze erneut auftritt
die nachlaufende Ebene. Der Long-Stop folgt dem Schlusskurs als Proxy für den Geldkurs, während der Short-Stop dem Schlusskurs folgt
ein Proxy für den Briefkurs.
7. Der EA verweigert den Handel, wenn `TakeProfitPoints` unter dem historischen 10-Punkte-Minimum liegt, was der Schutzprüfung entspricht
im MQL-Code vorhanden.

## Risikomanagement
- **Einzelauftrag nach dem anderen.** Die Strategie erfolgt stets Net-Flat, bevor eine neue Position eröffnet wird, und spiegelt das Original wider
`OrdersTotal() < 1` Anforderung.
- **Feste Lautstärke.** `TradeVolume` ersetzt die Eingabe `Lots` und wird auch nach `Strategy.Volume` kopiert, sodass manuelle UI-Aktionen verwendet werden
die gleiche Größe.
- **Fester Take-Profit.** `TakeProfitPoints` wandelt den Punktabstand MQL in die Tick-Größe des Instruments um
`Security.PriceStep`.
- **Indikatorbasierter Ausstieg.** Ein Vorzeichenwechsel im Histogramm löst einen sofortigen Marktausstieg aus und garantiert, dass der EA nicht drin bleibt
der Markt, wenn sich die Dynamik umkehrt.
- **Trailing Stop.** Sobald sich der Preis um mehr als die konfigurierte Anzahl von Schritten zugunsten des Handels bewegt, wird der Stop gezogen
innerhalb der Gewinnzone und folgt dem Schlusskurs, ohne sich dabei jemals rückwärts zu bewegen.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `10` | Auftragsgröße (Lots), die für jeden Markteintritt verwendet und nach `Strategy.Volume` kopiert wird. |
| `TakeProfitPoints` | `int` | `10` | Abstand zum festgelegten Take-Profit-Ziel, ausgedrückt in Preisschritten. Werte unter 10
Deaktivieren Sie den Handel. |
| `TrailingStopPoints` | `int` | `25` | Abstand in Preisschritten für den Trailing Stop. Auf `0` setzen, um das Nachstellen zu deaktivieren. |
| `FastPeriod` | `int` | `9` | Länge des schnellen EMA innerhalb des MACD-Indikators. |
| `SlowPeriod` | `int` | `15` | Länge des langsamen EMA innerhalb des MACD-Indikators. |
| `SignalPeriod` | `int` | `8` | Länge des EMA, der zum Glätten der MACD-Signallinie verwendet wird. |
| `ThresholdMultiplier` | `decimal` | `0.004` | Auf den vorherigen Balken ATR angewendeter Multiplikator, um das Ausbruchsband zu bilden. |
| `AtrPeriod` | `int` | `200` | Anzahl der Kerzen, die zur Berechnung des ATR-Volatilitätsfilters verwendet werden. |
| `CandleType` | `DataType` | 30-minütiger Zeitrahmen | Primärer Zeitrahmen, der von der Strategie verarbeitet wird. |

## Unterschiede zum ursprünglichen Fachberater
- MetaTrader macht `AccountFreeMargin()` sichtbar und verweigert den Handel, wenn der Wert zu gering ist. StockSharp-Strategien tun dies nicht
haben den gleichen Rand-Snapshot, daher lässt der Port diese Prüfung aus. Risikokontrollen auf Portfolioebene sollten außerhalb erfolgen
Strategie bei Bedarf.
- Die MQL-Version hat Stop-Orders mit `OrderModify` angepasst. StockSharp arbeitet mit Nettopositionen, sodass die Konvertierung verwaltet wird
Exits intern durch Überwachung der Kerzenhochs/-tiefs und der Trailing-Stop-Variablen.
- MetaTrader zählte „Balken“ manuell und gab eine Warnung aus, wenn weniger als 100 Kerzen verfügbar waren. StockSharp verlässt sich auf
Indikatorbereitschaft (`BindEx`), sodass die Strategie automatisch inaktiv bleibt, bis MACD und ATR genügend Daten haben.
- Der Port speichert die vorherigen ATR- und Histogrammwerte explizit, um den Schwellenwertvergleich `Delta`/`Delta1` zu reproduzieren
ohne gegen die Regel von StockSharp gegen die Indexierung zufälliger Indikatoren zu verstoßen.

## Anwendungstipps
- Sorgen Sie dafür, dass `Security.PriceStep`, `Security.MinVolume` und `Security.VolumeStep` genau sind, um mehr Conversions zu erzielen und Gewinne mitzunehmen
Die Berechnungen bleiben an der Börse ausgerichtet.
- Erhöhen Sie `ThresholdMultiplier` oder `AtrPeriod`, wenn die Strategie in unruhigen Märkten zu häufig handelt; verringern Sie sie auf
Machen Sie das System empfindlicher gegenüber Volatilitätsausbrüchen.
- Niedrigerer `TradeVolume` bei Verwendung von gehebelten Instrumenten oder Instrumenten mit hoher Volatilität, da das ursprüngliche Skript von einem hohen Wert ausging
Losgrößen auf Forex-Symbolen.
- Kombinieren Sie die Strategie mit Filtern für längere Zeiträume über die integrierte Eigenschaft `Direction`, wenn Sie nur zulassen möchten
Long- oder Short-Positionen während bestimmter Marktregime.
