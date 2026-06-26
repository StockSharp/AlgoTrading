# Cidomo-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruch-System konvertiert vom MetaTrader 5 Expert Advisor "Cidomo". Die Strategie wartet auf eine neue Kerze im konfigurierten Zeitrahmen, misst die jüngste Handelsspanne und platziert gepaarte Stop-Orders ober- und unterhalb dieser Spanne. Das Risiko wird mit klassischen Stop-Loss-/Take-Profit-Niveaus, einem optionalen Trailing Stop und zwei Geldverwaltungsmodi (festes Volumen oder prozentbasiertes Risiko) verwaltet.

## Funktionsweise

1. Bei jeder abgeschlossenen Kerze von `CandleType` werden die letzten `BarsCount` Hochs und Tiefs gesammelt, um den kurzfristigen Kanal zu definieren.
2. Eine Buy-Stop-Order wird bei `highest + IndentPips` und eine Sell-Stop-Order bei `lowest - IndentPips` platziert (beide Werte in Pips ausgedrückt und in absolute Preise umgerechnet).
3. Wenn eine Stop-Order ausgelöst wird, wird die entgegengesetzte ausstehende Order sofort storniert.
4. Für eine offene Position verfolgt die Strategie:
   - Anfänglichen Stop-Loss (`StopLossPips`) und Take-Profit (`TakeProfitPips`).
   - Einen schrittweisen Trailing Stop (`TrailingStopPips` / `TrailingStepPips`). Der Stop wird erst bewegt nachdem der Preis mindestens `TrailingStop + TrailingStep` vorschreitet, den ursprünglichen EA nachahmend.
   - Marktausstiege werden verwendet um MetaTraders `PositionModify`-Aufrufe zu emulieren wenn der Stop oder Take-Profit berührt wird.
5. Wenn `UseTimeFilter` aktiviert ist, werden neue Orders nur innerhalb von ±30 Sekunden von `StartHour:StartMinute` (Serverzeit) gesendet, was das enge Handelsfenster des Quellskripts repliziert.

## Geldverwaltung

- **FixedVolume**: handelt immer das vom Benutzer angegebene genaue `TradeVolume`.
- **RiskPercent**: berechnet die Ordergröße so, dass ein verlusttragender Trade bei der konfigurierten Stop-Loss-Distanz das Eigenkapital um `RiskPercent` reduziert. Volumina werden auf den `VolumeStep` des Instruments gerundet und zwischen `MinVolume` / `MaxVolume` begrenzt.

## Risikokontrollen

- Anfängliche Stop-Loss- und Take-Profit-Niveaus werden lokal gespeichert und über Marktorders ausgeführt wenn der Preis das Ziel während der nächsten Kerze kreuzt.
- Der Trailing Stop bewegt sich nur in eine Richtung und respektiert die Schrittweite vom ursprünglichen EA, was ständige kleine Anpassungen verhindert.
- Wenn kein Stop-Loss konfiguriert ist, fällt die risikobasierte Positionsgrößenbestimmung automatisch auf das feste `TradeVolume` zurück.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H4` | Zeitrahmen zur Bestimmung des Ausbruchsbereichs. |
| `BarsCount` | `int` | `15` | Anzahl abgeschlossener Kerzen zur Berechnung des höchsten Hochs und niedrigsten Tiefs. |
| `IndentPips` | `decimal` | `3` | Offset (in Pips) über/unter dem Bereich vor dem Senden von Stop-Orders. |
| `StopLossPips` | `decimal` | `50` | Schutz-Stop-Abstand in Pips. Wert `0` deaktiviert den Stop. |
| `TakeProfitPips` | `decimal` | `50` | Gewinnziel-Abstand in Pips. Wert `0` deaktiviert das Ziel. |
| `TrailingStopPips` | `decimal` | `35` | Trailing-Stop-Abstand in Pips. Auf `0` setzen um Trailing zu deaktivieren. |
| `TrailingStepPips` | `decimal` | `5` | Mindest-Extragewinn erforderlich vor dem Anziehen des Trailing Stops. |
| `MoneyManagement` | `CidomoMoneyManagementMode` | `RiskPercent` | Wählt zwischen fester Positionsgröße und risikobasierter Größenbestimmung. |
| `RiskPercent` | `decimal` | `1` | Prozentsatz des Eigenkapitals pro Trade riskiert wenn `MoneyManagement = RiskPercent`. |
| `TradeVolume` | `decimal` | `0.1` | Festes Order-Volumen im `FixedVolume`-Modus oder wenn risikobasierte Größenbestimmung nicht berechnet werden kann. |
| `UseTimeFilter` | `bool` | `false` | Aktiviert den ±30-Sekunden-Zeitfensterfilter. |
| `StartHour` | `int` | `9` | Stunde (0-23) des Handelsfenster-Zentrums. |
| `StartMinute` | `int` | `58` | Minute (0-59) des Handelsfenster-Zentrums. |

## Hinweise

- Alle pip-basierten Parameter passen sich automatisch an 3- oder 5-stellige Kurse an, indem der `PriceStep` des Instruments mit 10 multipliziert wird, genau wie die MetaTrader-Implementierung.
- Da StockSharp Stops clientseitig in diesem Port verwaltet, stelle sicher dass die Strategie verbunden bleibt, damit Marktausstiege ausgegeben werden können wenn Schutzniveaus durchbrochen werden.
