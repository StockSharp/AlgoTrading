# ADX System-DI-übergreifende Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die ADX-Systemstrategie ist die StockSharp-Konvertierung des MetaTrader 4-Experten `ADX_System.mq4`. Das Original EA vergleicht die
Durchschnittlicher Richtungsindex (ADX) mit seinen +DI- und -DI-Komponenten für die beiden zuletzt abgeschlossenen Kerzen. Wenn die +DI-Leitung
steigt über den ADX-Wert, den das System lang halten möchte; Wenn die -DI-Linie über den ADX-Wert steigt, möchte sie kurz sein. Die
Der Port StockSharp reproduziert dieses Verhalten, indem er die Indikatorwerte der beiden vorherigen fertigen Kerzen speichert, also die Logik
spiegelt die `iADX(..., shift=1/2)`-Aufrufe wider, die im MetaTrader-Code verwendet werden.

Es kann immer nur eine Position offen sein. Die Strategie übermittelt Marktaufträge für Ein- und Ausstiege, passend zum Einzelticket
Logik von MetaTrader Netting-Konten. Das Risikomanagement spiegelt den ursprünglichen Expertenberater wider: feste Take-Profit- und Stop-Loss-Werte
Die Niveaus werden in Punkten im Verhältnis zum durchschnittlichen Einstiegspreis ausgedrückt, und ein optionaler Trailing-Stop kann Gewinne sichern, sobald der Kurs erreicht ist
Position bewegt sich günstig.

## Handelslogik
1. Abonnieren Sie den konfigurierten Zeitrahmen (`CandleType`) und verarbeiten Sie nur fertige Kerzen, um Entscheidungen innerhalb des Balkens zu vermeiden.
2. Füttern Sie einen `AverageDirectionalIndex`-Indikator mit den Kerzendaten und warten Sie, bis der Indikator seine Werte ADX, +DI und -DI bereitstellt
Werte.
3. Speichern Sie die Indikatorwerte der beiden zuletzt abgeschlossenen Kerzen, damit die Strategie auf die „aktuellen“ und „aktuellen“ Werte verweisen kann
„vorherige“ Werte genau wie die MetaTrader-Implementierung.
4. **Langer Eintrag**: Wenn der ältere ADX (`shift = 2`) unter dem neueren ADX (`shift = 1`) liegt, liegt der ältere +DI darunter
ADX und der neuere +DI über dem neueren ADX liegt, senden Sie eine Marktkauforder.
5. **Kurzeintrag**: Wenn die gleichen Bedingungen für die -DI-Komponente auftreten (alter -DI unter altem ADX, neuer -DI über neuem ADX), senden Sie eine
Marktverkaufsauftrag.
6. **Long Exit**: Schließen Sie die Long-Position, wenn der ADX zu fallen beginnt und +DI wieder unter diesen Wert fällt, wenn die Konfiguration erfolgt
Take-Profit oder Stop-Loss erreicht wird oder wenn der Trailing Stop durchbrochen wird.
7. **Kurzer Exit**: Spiegeln Sie die Long-Exit-Logik mithilfe von -DI zusammen mit den Risikokontrollen wider.
8. Aktualisieren Sie den zwischengespeicherten Indikatorverlauf nach jeder Kerze, sodass das nächste Signal das neueste `shift = 1/2`-Paar verwendet.

## Risikomanagement
- `TakeProfitPoints` und `StopLossPoints` beschreiben Entfernungen in Punkten im MetaTrader-Stil. Sie werden in tatsächliche Preiseinheiten umgerechnet
Verwendung von `Security.PriceStep`, sofern verfügbar; andernfalls wird der Rohwert als absolutes Preisdelta behandelt.
- Der Trailing Stop (`TrailingStopPoints`) wird erst aktiviert, nachdem die Position mindestens den konfigurierten Abstand vom erreicht hat
Eintrittspreis. Sobald es aktiv ist, bewegt es sich in Richtung Gewinn und schließt die Position, wenn der Preis das nachlaufende Niveau überschreitet.
- Alle Exits (Indikatorumkehr, Take-Profit, Stop-Loss, Trailing Stop) verwenden Marktaufträge, sodass die Position abgeflacht wird
sofort und ahmt das Verhalten von `OrderClose` aus der Quelle EA nach.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Zeitrahmen von 1 Minute | Primärer Zeitrahmen, der von der Strategie verarbeitet wird. |
| `AdxPeriod` | `int` | `14` | Anzahl der Kerzen, die zur Berechnung der ADX- und DI-Komponenten verwendet werden. |
| `TradeVolume` | `decimal` | `1` | Für jede Marktorder verwendete Losgröße. |
| `TakeProfitPoints` | `decimal` | `100` | Take-Profit-Distanz in Punkten relativ zum Einstiegspreis. |
| `StopLossPoints` | `decimal` | `30` | Stop-Loss-Distanz in Punkten relativ zum Einstiegspreis. |
| `TrailingStopPoints` | `decimal` | `0` | Optionaler Trailing-Stop-Abstand in Punkten. Auf Null setzen, um das Nachziehen zu deaktivieren. |

## Unterschiede zum ursprünglichen MetaTrader-Experten
- MetaTrader verwaltet einzelne Tickets, während StockSharp mit einer einzelnen Nettoposition arbeitet. Die Konvertierung schließt daher die
Überprüfen Sie die aktuelle Position, bevor Sie einen neuen Eingabebefehl erteilen, wenn das Signal wechselt.
- Das ursprüngliche EA stützte sich auf `Point`, um Punkte in Preisentfernungen umzuwandeln. Der StockSharp-Port verwendet `Security.PriceStep`, wenn er
ist bekannt; andernfalls wird der Abstand als Rohpreiseinheit behandelt, sodass Sie möglicherweise die Standardeinstellungen für Instrumente mit anpassen müssen
unkonventionelle Preisschritte.
- MetaTrader wendet Trailing Stops an, indem es die bestehende Reihenfolge ändert. StockSharp schließt die Position mit einer Marktorder, wenn die
Der Trailing Stop wird verletzt, was im Netting-Modell funktional gleichwertig, aber einfacher ist.

## Anwendungstipps
- Stellen Sie sicher, dass das Strategievolumen (`TradeVolume`) mit dem Losschritt des Instruments übereinstimmt. Der Konstruktor weist diesen Wert auch zu
`Strategy.Volume`, daher verwenden Hilfsmethoden die erwartete Handelsgröße.
- Erhöhen Sie `TakeProfitPoints` und `StopLossPoints`, wenn Sie Instrumente mit größeren Durchschnittsspannen oder kleineren Preisschritten handeln.
- Fügen Sie die Strategie einem Diagramm hinzu, um die Kerzen, den ADX-Indikator und ausgeführte Trades zu visualisieren, was bei der Überprüfung dieser Signale hilft
treten genau dann auf, wenn +DI oder -DI die Linie ADX überschreitet.

## Indikatoren
- `AverageDirectionalIndex` (stellt ADX zusammen mit +DI- und -DI-Komponenten bereit).
