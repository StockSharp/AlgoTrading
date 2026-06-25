# Freeman-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Freeman ist eine Intraday-Strategie, die mehrere Momentum-Filter überlagert, um schrittweise in Trends einzusteigen. Sie verwendet zwei RSI-"Lehrer", die von gleitenden Durchschnitten auf dem Handelszeitrahmen angetrieben werden, zusammen mit einem höheren Zeitrahmen-Gleitender-Durchschnitt-Filter. Das Risiko wird durch ATR-basierte Stop-Loss- und Take-Profit-Ziele sowie einen pip-basierten Trailing-Stop kontrolliert.

## Strategie-Überblick

- Funktioniert auf beliebigen Zeitrahmen-Kerzen, die durch den Parameter `CandleType` ausgewählt werden (standardmäßig 15 Minuten).
- Verwendet einen stündlichen Filter (`FilterCandleType`), um Trends zu qualifizieren, bevor Signale akzeptiert werden.
- Erstellt Long- und Short-Signale aus zwei RSI-Blöcken, die aktuelle und vorherige Werte in Kombination mit Gleitender-Durchschnitt-Neigungen vergleichen.
- Erlaubt Pyramiding, wenn der Markt sich weiter bewegt, mit der Option, die nächste Order nach einem Verlustaustieg zu vergrößern.

## Handelslogik

### Long-Bedingungen

1. Der höhere Zeitrahmen-Filter ist optional. Wenn aktiviert, muss der stündliche gleitende Durchschnitt aufwärts geneigt sein.
2. RSI Lehrer #1 ist aktiv, wenn:
   - RSI #1 auf dem vorherigen Balken unter `RsiSellLevel` lag und auf dem aktuellen Balken steigt.
   - Der schnelle gleitende Durchschnitt steigt.
   - Der stündliche RSI (Periode 14) unter `RsiBuyLevel` bleibt, um zu bestätigen, dass der höhere Zeitrahmen nicht überkauft ist.
3. RSI Lehrer #2 ist aktiv, wenn:
   - RSI #2 unter `RsiSellLevel2` lag und nach oben dreht.
   - Der langsame gleitende Durchschnitt steigt.
   - Der stündliche RSI unter `RsiBuyLevel2` bleibt.
4. Ein Long-Einstieg wird genommen, wenn mindestens ein Lehrer aktiv ist und der Trendfilter (wenn aktiviert) übereinstimmt.
5. Weitere Long-Einstiege erfordern, dass sich der Schlusskurs mehr als `DistancePips` (umgerechnet durch den Preisschritt des Instruments) vom letzten Long-Fill entfernt. Wenn der letzte Long-Ausstieg ein Verlust war, wird das Volumen mit `LockCoefficient` multipliziert, um das MT5-Sperrverhalten nachzuahmen.

### Short-Bedingungen

Spiegelt die Long-Logik mit invertierten Vergleichen:

- Der höhere Zeitrahmen-Gleitende-Durchschnitt muss sinken, wenn der Filter aktiviert ist.
- RSI Lehrer #1 benötigt RSI #1 über `RsiBuyLevel`, der nach unten dreht, den schnellen MA fallend und den stündlichen RSI über `RsiSellLevel`.
- RSI Lehrer #2 benötigt RSI #2 über `RsiBuyLevel2`, der nach unten dreht, den langsamen MA fallend und den stündlichen RSI über `RsiSellLevel2`.
- Weitere Short-Einstiege folgen denselben Abstands- und Sperrregeln.

## Positionsmanagement

- Stop-Loss und Take-Profit werden für jeden Einstieg aus dem aktuellen ATR-Wert unter Verwendung von `StopLossAtrFactor` und `TakeProfitAtrFactor` neu berechnet.
- Der Trailing-Stop aktiviert sich, sobald der Preis über `TrailingStopPips + TrailingStepPips` hinausgeht, und sichert dann Gewinne, indem der Stop `TrailingStopPips` vom letzten Schluss entfernt gehalten wird.
- Ausstiege werden mit Marktorders ausgeführt, sobald das Hoch/Tief der Kerze die berechneten Stop- oder Zielniveaus durchbricht.
- Der Parameter `PositionsMaximum` begrenzt die Gesamtzahl der ausgeführten Einstiege (Long plus Short). Ein Wert von null entfernt die Begrenzung.

## Zeitfilter

- Der Handel an Freitagen kann durch `TradeOnFriday` deaktiviert werden.
- `StartHour` und `EndHour` definieren ein optionales Sitzungsfenster in Börsenzeit; Nullwerte halten den Markt den ganzen Tag geöffnet.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `CandleType` | Handelszeitrahmen für die Hauptsignallogik. |
| `FilterCandleType` | Höherer Zeitrahmen für den Gleitender-Durchschnitt- und RSI-Filter (Standard 1 Stunde). |
| `FirstMaPeriod` / `SecondMaPeriod` | Perioden für die schnellen und langsamen gleitenden Durchschnitte, die die RSI-Lehrer speisen. |
| `FilterMaPeriod` | Länge des höheren Zeitrahmen-Gleitender-Durchschnitts. |
| `MaType` | Gleitender-Durchschnitt-Typ (SMA, EMA, SMMA oder WMA). |
| `RsiFirstPeriod` / `RsiSecondPeriod` | Perioden der beiden RSI-Lehrer. |
| `RsiSellLevel`, `RsiBuyLevel`, `RsiSellLevel2`, `RsiBuyLevel2` | RSI-Schwellenwerte zur Steuerung der Lehrerblöcke. |
| `UseRsiTeacher1`, `UseRsiTeacher2`, `UseTrendFilter` | Schalter für jede Komponente. |
| `StopLossAtrFactor`, `TakeProfitAtrFactor` | ATR-Multiplikatoren für Stop-Loss- und Take-Profit-Abstände. |
| `TrailingStopPips`, `TrailingStepPips` | Pip-Offsets für die Trailing-Stop-Engine. |
| `PositionsMaximum` | Maximale Anzahl kombinierter Einstiege; null = unbegrenzt. |
| `DistancePips` | Mindest-Pip-Abstand vor dem Hinzufügen zu einer Position. |
| `TradeOnFriday` | Signale an Freitagen aktivieren oder deaktivieren. |
| `StartHour`, `EndHour` | Optionale Handelssitzungslimits. |
| `LockCoefficient` | Volumen-Multiplikator nach einem Verlustausstieg beim Stapeln in dieselbe Richtung. |
| `SignalShift` | Offset beim Lesen von Indikatorwerten (0 = aktueller fertiger Balken). |

## Implementierungshinweise

- Der StockSharp-Port verarbeitet nur fertige Kerzen und entspricht dem MT5-"Bars Control"-Verhalten, auch wenn das Original Tick-basierten Handel ermöglichte.
- In Pips ausgedrückte Preisabstände werden mit dem `PriceStep` des Instruments umgerechnet.
- Die Schutzlogik (Stop, Ziel, Trailing) schließt Positionen mit Marktorders, da High-Level-API-Bindings anstelle einzelner MT5-Positionsmodifikationen verwendet werden.
- Die Strategie führt aggregierte Long- und Short-Volumen; einmal geschlossen, setzt die Verlustverfolgung zurück, sodass das nächste Signal wie die ursprünglichen Sperrregeln funktioniert.

Verwenden Sie geeignete Risikokontrollen und testen Sie gründlich, bevor Sie an Live-Märkten handeln.
