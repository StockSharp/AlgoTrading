# Expert Candles-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Expert Candles Strategy** ist eine StockSharp-Portierung des MetaTrader 5 *Expert_Candles* Expertenberaters. Es überwacht am meisten
jüngste Preisbewegung für Candlestick-Umkehrformationen, die längliche Schatten aufweisen. Wann immer eine bullische oder bärische Zusammensetzung vorliegt
Wenn eine Kerze erkannt wird, eröffnet die Strategie eine Position in die entsprechende Richtung und wendet optional das gleiche Geldmanagement an
das Original EA.

Die Implementierung folgt dem High-Level-StockSharp API: Kerzenabonnements werden verwendet, um zusammengesetzte Balken zu erstellen und gleichzeitig zu vermarkten
Aufträge und Schutzstufen werden direkt aus der Strategie verwaltet.

## Handelslogik

1. Jedes Mal, wenn eine Kerze geschlossen wird, führt die Strategie sie mit bis zu `Range` vorherigen Kerzen zusammen, bis die volle Höhe des Verbunds erreicht ist
Der Balken überschreitet `MinimumPoints` (umgerechnet in Preispunkte unter Verwendung der Pip-Größe des Instruments).
2. Ein **bullisches** Signal wird ausgegeben, wenn der zusammengesetzte Balken einen flachen oberen Schatten (`ShadowSmall`) und einen tiefen unteren Schatten aufweist
(`ShadowBig`). Ein **bärisches** Signal wird ausgegeben, wenn der untere Schatten flach und der obere Schatten dominant ist.
3. Der Einstiegspreis wird von der Kerze um `LimitFactor * rangeSize` verschoben. Positive Werte emulieren den ursprünglichen Grenzwert
Ordnung, die innerhalb des Kerzenbereichs liegt.
4. Stop-Loss- und Take-Profit-Ziele werden bei `StopLossFactor`- und `TakeProfitFactor`-Vielfachen der zusammengesetzten Höhe positioniert.
Wenn eines der beiden Niveaus bei nachfolgenden Kerzen erreicht wird, wird die Position sofort geschlossen.
5. Signale gelten für `ExpirationBars` abgeschlossene Kerzen als gültig. Sobald das Zeitfenster verstrichen ist, wartet die Strategie auf ein neues
Formation, bevor Sie neue Bestellungen aufgeben.
6. Entgegengesetzte Signale schließen bestehende Positionen, bevor Trades in die neue Richtung eingeleitet werden, und ahmen das Verhalten von MQL5 nach.

## Geldmanagement

* Als Standardbestellgröße wird `FixedVolume` verwendet.
* Wenn ein Stop-Loss verfügbar ist und `RiskPercent` größer als Null ist, riskiert die Strategie den ausgewählten Prozentsatz
Portfolio-Eigenkapital. Die Stoppdistanz wird mithilfe von `Security.PriceStep` und `Security.StepPrice` in einen Geldwert umgerechnet.
* Die Volumina werden auf das Instrument `VolumeStep` gerundet, wenn die Börse diese Metadaten offenlegt.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | H1 | Zeitrahmen, der zum Anfordern von Kerzen verwendet wird. |
| `Range` | 3 | Maximale Anzahl benachbarter Kerzen, die zu einem zusammengesetzten Muster kombiniert werden. |
| `MinimumPoints` | 50 | Minimale zusammengesetzte Höhe in Punkten (`PriceStep`-basiert), die zur Auswertung des Musters erforderlich ist. |
| `ShadowBig` | 0,5 | Verhältnis, das der dominante Schatten überschreiten muss, um die Umkehrung zu bestätigen. |
| `ShadowSmall` | 0,2 | Maximal zulässiges Verhältnis für den gegenüberliegenden Schatten. |
| `LimitFactor` | 0,0 | Einstiegsoffset als Bruchteil der zusammengesetzten Höhe (positive Werte verschieben den Preis innerhalb der Kerze). |
| `StopLossFactor` | 2,0 | Stop-Loss-Distanz als Vielfaches der zusammengesetzten Höhe. Auf Null setzen, um den Schutzstopp zu deaktivieren. |
| `TakeProfitFactor` | 1,0 | Take-Profit-Distanz als Vielfaches der zusammengesetzten Höhe. Auf Null setzen, um das Ziel zu deaktivieren. |
| `ExpirationBars` | 4 | Anzahl der abgeschlossenen Kerzen, während derer ein Signal aktiv bleibt. |
| `FixedVolume` | 0,1 | Fallback-Ordergröße, die verwendet wird, wenn die risikobasierte Größenbestimmung nicht berechnet werden kann. |
| `RiskPercent` | 10 | Prozentsatz des pro Trade riskierten Eigenkapitals, wenn ein Stop-Loss verfügbar ist. |

## Nutzungshinweise

- Die Strategie basiert auf `Security.PriceStep`, `Security.StepPrice` und `Security.VolumeStep`, um den Punkt MetaTrader zu replizieren
Berechnungen. Stellen Sie genaue Instrumentenmetadaten bereit oder passen Sie die Parameter entsprechend an.
- Signale werden nur bei geschlossenen Kerzen ausgewertet. Hängen Sie die Strategie an einen Zeitreihen-Connector an, der `CandleStates.Finished` ausgibt.
Veranstaltungen für eine zuverlässige Durchführung.
- Schutzausstiege werden simuliert, indem die Position geschlossen wird, sobald das Hoch oder Tief einer fertigen Kerze den berechneten Wert überschreitet
Stop-Loss- oder Take-Profit-Level.
- Die zusammengesetzte Kerzenliste ist auf 500 Elemente begrenzt, um den Speicherbedarf vorhersehbar zu halten.

## Unterschiede zur MetaTrader-Version

- Der Port StockSharp verwendet Marktaufträge anstelle ausstehender Limitaufträge. Der Eintragsoffset gibt das Grenzverhalten wieder
Verschiebung des Ausführungspreises relativ zum Kerzenschluss.
- Die Geldverwaltung ist optional; Wenn Sie `RiskPercent` auf Null setzen, wird das feste Losverhalten vom ursprünglichen EA wiederhergestellt.
- Die Stop-Loss- und Take-Profit-Abwicklung erfolgt innerhalb der Strategie und nicht durch externe Trailing-Module.
