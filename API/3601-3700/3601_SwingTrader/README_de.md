# SwingTrader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **SwingTrader-Strategie** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `SwingTrader.mq4`. Das Original EA sucht
Bollinger Bandumkehrungen: Wenn der Preis vom äußeren Band abprallt und der nächste Balken die Mittellinie kreuzt, öffnet der Berater eine
Position und beginnt mit dem Aufbau eines Mittelungsgitters im Martingal-Stil. Die übersetzte Strategie reproduziert das gleiche Verhalten auf hoher Ebene
unter Verwendung von StockSharp-Kerzen, Bollinger-Bändern aus `StockSharp.Algo.Indicators` und den Ordnungshelfern des Frameworks (`BuyMarket`,
`SellMarket`). Die Volumenskalierung, die Breite des Rasters und die Liquidationsregeln spiegeln den MT4-Code unter Berücksichtigung der Börse wider
Grenzwerte, die durch die `Security`-Metadaten bereitgestellt werden.

## Handelslogik
1. Abonnieren Sie den konfigurierten Zeitrahmen (`CandleType`) und füttern Sie einen Bollinger-Bandindikator mit einer Länge von `BollingerPeriod` und einem
fester Standardabweichungsmultiplikator von `2`.
2. Arbeiten Sie nur mit fertigen Kerzen; Der Indikator-Callback ignoriert teilweise geformte Balken, um den MT4 `IsNewCandle()` zu reproduzieren.
Wache.
3. Verfolgen Sie, ob die vorherige Kerze das obere oder untere Band berührt hat. Das boolesche Paar `_upTouch` / `_downTouch` folgt dem
Original-Umschaltlogik, die nur eine Seite aktiv hält, bis das gegenüberliegende Band berührt wird.
4. Wenn kein Warenkorb geöffnet ist:
   - Eröffnen Sie eine Long-Position, wenn der letzte abgeschlossene Balken das mittlere Band überschreitet, nachdem er zuvor das untere Band berührt hat.
   - Eröffnen Sie eine Short-Position, wenn der Balken das mittlere Band unterschritten hat, nachdem er das obere Band berührt hat.
Das Volumen erster Ordnung entspricht `InitialVolume` (nach Börsenrundung) und die anfängliche Gitterbreite entspricht der letzten Entfernung
zwischen dem oberen und unteren Bollinger-Band.
5. Wenn ein Korb vorhanden ist, achten Sie ab der ersten Füllung auf eine nachteilige Bewegung um eine volle Bandbreite:
   - Wenn bei Long-Positionen das Tief der Kerze mindestens eine Bandbreite unter dem Ankerpreis liegt, kaufen Sie ein weiteres Segment, dessen Größe vervielfacht wird
um `Multiplier` mit jedem neuen Level;
   - Wenn bei Short-Positionen das Hoch der Kerze eine Bandbreite über dem Ankerpreis liegt, verkaufen Sie ein zusätzliches Stück davon
Multiplikatorlogik.
6. Sammeln Sie so lange neue Aufträge, bis entweder das Gewinn- oder das maximal tolerierte Verlustziel erreicht ist.

## Geldmanagement und Exits
- Der Helfer `CalculateUnrealizedProfit` reproduziert die MT4-Floating-PnL-Berechnung, indem er Preisunterschiede in Preise umwandelt
Schritte (`Security.PriceStep`) und Schrittwert (`Security.StepPrice`).
- Der Proxy für das investierte Kapital verwendet die ursprüngliche Formel `Lots * Price / TickSize * TickValue / 30`, wobei `Lots` die Summe ist
der Gittervolumina und die Tick-Parameter stammen aus `Security`.
- Schließen Sie den gesamten Warenkorb, sobald der variable Gewinn `TakeProfitFactor * invested capital` übersteigt.
- Erzwingen Sie eine Notfallliquidation, wenn der schwebende Verlust `10 * TakeProfitFactor * invested capital` erreicht (gleiches Verhältnis wie
MT4-Code).
- Alle Exits werden mit Marktaufträgen in die entgegengesetzte Richtung ausgeführt; Sobald es flach ist, wird der Gitterzustand zurückgesetzt und es müssen neue Berührungen vorgenommen werden
erkannt, bevor ein weiterer Eintrag ausgelöst werden kann.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `TakeProfitFactor` | `decimal` | `0.05` | Multiplikator, der auf das investierte Kapital angewendet wird, um das Gewinnziel zu definieren. |
| `Multiplier` | `decimal` | `1.5` | Volumenmultiplikator für jeden weiteren Mittelungsauftrag. |
| `BollingerPeriod` | `int` | `20` | Anzahl der Kerzen, die vom Bollinger Bands-Indikator verwendet werden. |
| `InitialVolume` | `decimal` | `1` | Basisvolumen des ersten Handels in einem neuen Korb (gerundet auf die Handelsplatzgrenzen). |
| `CandleType` | `DataType` | 15-minütiger Zeitrahmen | Zeitrahmen, der für die Signalgenerierung verwendet wird. |

## Unterschiede zum Original EA
- StockSharp funktioniert mit Nettopositionen; Die Strategie verwaltet explizite Listen von Rastereinträgen, um die Ticket-basierte Reihenfolge von MT4 zu emulieren
Handhabung.
- Stattdessen werden Exchange-Volumenfilter (`Security.MinVolume`, `Security.VolumeStep`, `Security.MaxVolume`) automatisch angewendet
des manuellen Aufrufs von `CheckVolumeValue`.
- Signale werden bei geschlossenen Kerzen ausgewertet; Intrabar-Trigger aus der MT4-Version werden durch die Verwendung von Kerzenhochs und -tiefs angenähert
für Mittelungsentscheidungen.
- Aufträge werden immer als Marktanweisungen gesendet, während MT4 `OrderSend` mit expliziten Bid/Ask-Parametern verwendet.

## Nutzungshinweise
- Stellen Sie realistische Metadaten für das gehandelte Instrument bereit: `PriceStep`, `StepPrice`, `MinVolume`, `VolumeStep` und `MaxVolume` müssen
für die Gewinn-, Verlust- und Volumenberechnungen ausgefüllt werden, damit sie dem MT4-Verhalten entsprechen.
- Da das Durchschnittsraster geometrisch skaliert wird, testen Sie die Konfiguration anhand historischer Daten und berücksichtigen Sie die Broker-Marge
Informieren Sie sich über die Anforderungen, bevor Sie es live ausführen.
- Die Gitterbreite entspricht der aktuellen Bollinger-Bandbreite; Die Änderung von `BollingerPeriod` wirkt sich direkt auf den Eintrittszeitpunkt und das Raster aus
Abstand. Validieren Sie die Empfindlichkeit während der Optimierung.
