# Steigung RSI MTF-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Slope RSI MTF-Strategie** portiert den MetaTrader 4 Expertenberater `SLOPE_RSI_MTF_LBranjord.mq4` zusammen mit seinem Begleitindikator `Slope_Direction_Line_Alert.mq4`. Das ursprüngliche Setup stapelte mehrere gleitende Hull-Durchschnitte (genannt „Slope Direction Line“) über mehrere Zeitrahmen und eröffnete nur Geschäfte, wenn alle in die gleiche Richtung zeigten, während ein vierstufiger RSI-Filter die Dynamik bestätigte. Die StockSharp-Version reproduziert diese Multi-Timeframe-Bestätigungslogik mit High-Level-Abonnements, behält die ATR-basierten Exit-Ziele bei und fügt umfangreiche Konfigurationsunterstützung durch Strategieparameter hinzu.

## Handelslogik
1. Abonnieren Sie vier Kerzenserien für dasselbe Instrument: den Handelszeitrahmen (`BaseTimeframe`), eine stündliche Bestätigungsserie, eine vierstündige Serie und eine tägliche Serie.
2. Füttere jede Serie in ihre eigene `HullMovingAverage` (den StockSharp-Ersatz für die Neigungsrichtungslinie) und `RelativeStrengthIndex`-Instanz. Die Basisserie verwendet `SlopeTriggerLength` (Standard 60), während die Bestätigungsserie `SlopeTrendLength` (Standard 200) verwendet.
3. Verfolgen Sie die letzten beiden Rumpfwerte pro Zeitrahmen. Ein Zeitrahmen gilt als bullisch, wenn der aktuelle Hull-Wert deutlich über dem vorherigen liegt; es ist bärisch, wenn der Hull-Wert deutlich unter dem vorherigen Wert liegt.
4. Überwachen Sie gleichzeitig die RSI in jedem Zeitrahmen:
   - Lange Einrichtung: RSI muss bei allen vier Serien über `RsiMiddleLevel` (standardmäßig 50), aber unter `RsiUpperBound` (90) liegen.
   - Kurzer Aufbau: RSI muss bei allen vier Serien unter `RsiMiddleLevel`, aber über `RsiLowerBound` (10) liegen.
5. Wenn der Basiszeitrahmen schließt und alle Bestätigungen bullisch sind, lösen Sie ein Long-Signal aus. Wenn alle Bestätigungen bärisch sind, lösen Sie ein Short-Signal aus. Signale werden ignoriert, bis jeder Indikator mindestens einen historischen Wert erzeugt hat.
6. Berechnen Sie vor dem Hinzufügen einer neuen Position die Schutzabstände anhand der Werte ATR:
   - Die stündliche Serie liefert die Stop-Loss-Distanz.
   - Die tägliche Serie liefert die Take-Profit-Distanz.
7. Markteintritte erhöhen das Engagement in Signalrichtung und respektieren dabei `MaxOrders`. Im Netting-Umfeld wird das gegenteilige Risiko abgeflacht, bevor ein neuer Trade hinzugefügt wird.
8. Die Schutzniveaus werden bei jedem Scale-In neu berechnet und bei nachfolgenden Kerzen im Basiszeitrahmen bewertet. Wenn das Hoch/Tief der Kerze das gespeicherte Stop-Loss- oder Take-Profit-Niveau überschreitet, verlässt die Strategie die gesamte Position mit einer Marktorder.

## Risikomanagement und Positionsgrößenbestimmung
- `UseCompounding` aktiviert die Aufzinsungsregel vom MQL-Experten: `volume = PortfolioValue / BalanceDivider`. Bei Deaktivierung wird stattdessen `BaseVolume` verwendet.
- Der Helfer `AdjustVolume` rundet das angeforderte Volumen auf den `VolumeStep` des Wertpapiers und erzwingt `MinVolume`/`MaxVolume`. Der angepasste Wert wird auch in `Strategy.Volume` geschrieben, sodass manuelle Aktionen dieselbe Größe haben.
- Der Zeitraum ATR (`AtrPeriod`, Standard 21) spiegelt die ursprünglichen Einstellungen für Stop-Loss- und Take-Profit-Berechnungen wider. Der Stopp verwendet den stündlichen ATR, während das Gewinnziel den täglichen ATR verwendet.
- Positionszähler (`_longEntries`, `_shortEntries`) stellen sicher, dass nicht mehr als `MaxOrders` Scale-Ins gleichzeitig in eine Richtung aktiv sind.

## Datenverarbeitung in mehreren Zeitrahmen
- Alle Abonnements werden mit `SubscribeCandles(...)` erstellt und über `Bind` verarbeitet. Die Strategie speichert historische Kerzen nicht manuell zwischen; Indikatoren reagieren auf Streaming-Daten und legen ihre endgültigen Werte über die `Bind`-Rückrufe offen.
- Der `TimeframeState`-Helfer speichert Hull- und RSI-Werte zusammen mit dem vorherigen Hull-Messwert und ermöglicht so Steigungsvergleiche, ohne historische Indikatorpuffer anzufordern.
- ATR-Werte werden nur verwendet, wenn der entsprechende Indikator `IsFormed` meldet, wodurch sichergestellt wird, dass Stopps und Ziele aus vollständigen Balken berechnet werden.

## Parameter
| Name | Typ | Standard | MetaTrader Gegenstück | Beschreibung |
| --- | --- | --- | --- | --- |
| `SlopeTriggerLength` | `int` | `60` | `SDL1_trigger` | Rumpflänge im Handelszeitraum. |
| `SlopeTrendLength` | `int` | `200` | `SDL1_period` | Rumpflänge bei stündlichen, vierstündigen und täglichen Bestätigungen. |
| `RsiPeriod` | `int` | `14` | RSI Zeitraum | RSI Lookback wird auf jeden Zeitrahmen angewendet. |
| `RsiLowerBound` | `decimal` | `10` | RSI Untergrenze | Niedrigerer RSI-Filter für kurze Signale. |
| `RsiMiddleLevel` | `decimal` | `50` | RSI mittleres Niveau (implizit) | Neutrales RSI-Niveau, das Long- und Short-Regime trennt. |
| `RsiUpperBound` | `decimal` | `90` | RSI Obergrenze | Oberer RSI-Filter für lange Signale. |
| `AtrPeriod` | `int` | `21` | `ATR_Period` | ATR Länge für Stop- und Take-Profit-Berechnungen. |
| `MaxOrders` | `int` | `5` | `MaxOrders` | Maximale Anzahl von Scale-In-Einträgen pro Richtung. |
| `UseCompounding` | `bool` | `true` | `compounding` | Ermöglicht eine portfoliobasierte Positionsgrößenbestimmung. |
| `BaseVolume` | `decimal` | `0.1` | `Lots` | Festes Los, wenn die Aufzinsung deaktiviert ist. |
| `BalanceDivider` | `decimal` | `100000` | implizit (`AccountBalance()/100000`) | Teiler für die Compoundierungsformel. |
| `BaseTimeframe` | `DataType` | `5m` | Zeitrahmen des Diagramms | Kerzenserie, die die Handelsausführung vorantreibt. |
| `HourTimeframe` | `DataType` | `1h` | `PERIOD_H1` | Erste Bestätigungsserie. |
| `FourHourTimeframe` | `DataType` | `4h` | `PERIOD_H4` | Zweite Bestätigungsserie. |
| `DayTimeframe` | `DataType` | `1d` | `PERIOD_D1` | Höchste Bestätigungsserie. |

## Unterschiede zum ursprünglichen Fachberater
- StockSharp arbeitet im Netting-Modus, sodass entgegengesetzte Positionen geschlossen werden, bevor ein neuer Handel eröffnet wird. MetaTrader 4 ermöglichte die Absicherung mehrerer Tickets in beide Richtungen.
- Schutzstopps und -ziele werden durch kerzenbasierte Überwachung statt durch maklerseitige Auftragsänderungen ausgeführt. Dadurch bleibt die Logik innerhalb der Strategie erhalten, während die ATR-Abstände des Originals EA reproduziert werden.
- Indikatorwerte werden von den integrierten Funktionen `HullMovingAverage`, `RelativeStrengthIndex` und `AverageTrueRange` von StockSharp bereitgestellt. Es wird nicht direkt auf benutzerdefinierte Indikatorpuffer zugegriffen, was den allgemeinen Best Practices von API entspricht.
- Parametermetadaten, lokalisierungsfreundliche Namen und Bereichshinweise werden über `Param(...).SetDisplay(...)` verfügbar gemacht, wodurch die Strategie einfacher zu konfigurieren und zu optimieren ist.

## Nutzungshinweise
- Achten Sie darauf, dass die Bestätigungszeiträume unbedingt größer oder gleich dem Handelszeitraum sind. Das Mischen kürzerer Zeiträume kann zu widersprüchlichen Signalen führen und den Zweck der Steigungsbestätigung für mehrere Zeitrahmen zunichte machen.
- Stellen Sie sicher, dass die Sicherheitsmetadaten (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) ausgefüllt sind, damit Stopp-/Zielrundung und Volumenanpassungen korrekt funktionieren.
- Da die Stop-Loss- und Take-Profit-Überwachung einmal pro abgeschlossener Basiskerze erfolgt, erfolgen Intrabar-Exits beim nächsten Balkenschluss. Wenn ein strengeres Intrabar-Management erforderlich ist, verkürzen Sie den Handelszeitraum oder erweitern Sie die Strategie um eine Überwachung auf Tick-Ebene.
- Der Hull-Steigungstest erfordert, dass aufeinanderfolgende Werte unterschiedlich sind. Flache Hull-Sequenzen (gleiche Werte) blockieren neue Trades, selbst wenn die RSI-Filter erfolgreich sind, und spiegeln die Bedingung „SDL > SDL[1]“ aus dem MetaTrader-Skript wider.
