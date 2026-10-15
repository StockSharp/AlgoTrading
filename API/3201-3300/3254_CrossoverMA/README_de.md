# CrossoverMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist ein StockSharp-Port des MetaTrader 5-Expertenberaters **CrossoverMA.mq5**. Der ursprüngliche Roboter wartet darauf, dass eine Kerze einen gleitenden Durchschnitt kreuzt, und öffnet eine Position nur, wenn der Durchschnitt in die gleiche Richtung wie der Ausbruch geneigt ist. Die StockSharp-Version behält dasselbe Verhalten bei und nutzt die High-Level-API für Kerzenabonnements, Indikatorverwaltung und automatisches Chart-Rendering.

## Handelslogik

1. Abonnieren der konfigurierten Kerzenserie und Berechnung eines einfachen gleitenden Durchschnitts (SMA) über den Kerzenschlusskurs.
2. Wenn eine abgeschlossene Kerze empfangen wird, messen:
   - Die Kerzeneröffnungs- und -schlussabstände vom SMA.
   - Die Steigung des SMA durch Vergleich des aktuellen Wertes mit dem vorherigen.
3. Signale generieren:
   - **Bullischer Ausbruch** – die Kerze öffnet unter dem SMA, schließt über ihm, und der SMA steigt. Die Strategie schließt jede Short-Exposition und öffnet/erweitert eine Long-Position.
   - **Bärischer Ausbruch** – die Kerze öffnet über dem SMA, schließt unter ihm, und der SMA fällt. Die Strategie schließt jede Long-Exposition und öffnet/erweitert eine Short-Position.
4. Doppelte Signale ignorieren, die die aktuelle Positionsseite nicht ändern.

Der Port behält die MetaTrader-Regel bei, dass nur abgeschlossene Kerzen verarbeitet werden und eine zusätzliche Kerze vor dem ersten Trade benötigt wird (um die SMA-Steigung zu messen).

## Parameter

| Name | Beschreibung | Standard | Hinweise |
| ---- | ----------- | ------- | ----- |
| `Candle Type` | Zeitrahmen für den Kerzenaufbau. | 1-Minuten-Zeitrahmen | Es kann jeder von StockSharp unterstützte Kerzendatentyp ausgewählt werden. |
| `MA Length` | Anzahl abgeschlossener Kerzen im SMA. | 12 | Entspricht der Standardperiode des MetaTrader-Experten. |
| `Trade Volume` | Market-Order-Volumen für Einstiege. | 1 | Die Strategie schließt die entgegengesetzte Exposition vor dem Öffnen einer neuen Position. |

Alle Parameter stehen für die Optimierung in StockSharp Designer oder Runner zur Verfügung.

## Implementierungshinweise

- Die Strategie basiert auf `SubscribeCandles` und `Bind`, sodass Indikatorwerte direkt in die Verarbeitungsmethode gestreamt werden, ohne manuelle Historienverwaltung.
- Der SMA wird in einem privaten Feld gespeichert, um ihn im Diagrammbereich zu zeichnen, wenn einer verfügbar ist.
- Signale werden nur verarbeitet, wenn `IsFormedAndOnlineAndAllowTrading()` `true` zurückgibt, sodass die Strategie den globalen Handelszustand respektiert.
- Positions-Umkehrungen folgen der MetaTrader-Vorlage: zuerst die aktuelle Exposition schließen, dann die neue Seite mit dem konfigurierten Trade-Volumen öffnen.

## Dateien

- `CS/CrossoverMaStrategy.cs` – C#-Implementierung der konvertierten Strategie.
- `README.md` – englische Dokumentation.
- `README_zh.md` – chinesische Dokumentation.
- `README_ru.md` – russische Dokumentation.

## Portierungsunterschiede

- Geldmanagement-, Trailing-Stop- und andere MetaTrader-Framework-Klassen werden weggelassen, da StockSharp Positionsgrößen und Risiken extern verwaltet. Der Parameter `Trade Volume` ersetzt die festen Lot-Einstellungen des ursprünglichen Experten.
- MetaTrader verwendete separate Datenserien für Kerzen-Öffnungs- und -Schlusspreise. StockSharp-Kerzen enthalten bereits beide Preise, sodass keine zusätzlichen Indikatoren erforderlich sind.
- Indikatorinitialisierung, -validierung und Lebenszyklusverwaltung werden automatisch von StockSharp gehandhabt, wodurch der umfangreiche Boilerplate-Code der MQL-Version entfällt.
