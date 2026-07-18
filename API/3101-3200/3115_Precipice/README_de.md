# Precipice-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Precipice-Strategie ist eine direkte Konvertierung des MetaTrader-Expertenberaters *Precipice (barabashkakvn's edition)*. Das System analysiert keine Marktstruktur und verwendet keine Indikatoren; stattdessen wartet es, bis die vorherige Position geschlossen ist, und wirft dann eine Münze, um zu entscheiden, ob es long oder short einsteigen soll. Wenn der Trader beide Richtungen aktiviert, hat jede abgeschlossene Kerze eine 50%ige Chance, eine neue Position zu erzeugen, sofern das Konto flat ist. Optionale Schutzorders spiegeln das MetaTrader-Verhalten, indem dieselbe Stop-Loss- und Take-Profit-Distanz in "Pips" an jeden Trade angehängt wird.

Die StockSharp-Implementierung behält die zufällige Natur des ursprünglichen Codes bei und reproduziert dessen Geldmanagement-Einstellungen. Sie wandelt die MetaTrader-Pip-Distanz automatisch in den nativen Preisschritt des Instruments um, sodass Stop-Loss und Take-Profit unabhängig von der Anzahl der Dezimalstellen des Wertpapiers symmetrisch bleiben.

## Handelslogik
1. Die primäre Kerzenserie abonnieren, die durch `CandleType` definiert ist, und nur abgeschlossene Kerzen verarbeiten, damit das Trade-Timing der MetaTrader `OnTick`-Logik nach dem Kerzenschluss entspricht.
2. Alle Signale ignorieren, solange eine Position offen ist. Der Experte platziert höchstens einen Trade gleichzeitig.
3. Wenn die Strategie flat ist, eine Zufallszahl für den Kauf-Zweig generieren. Wenn `UseBuy` aktiviert ist und der Wert unter 0.5 liegt, eine Kaufmarktorder mit `TradeVolume` Lots senden.
4. Wenn keine Long-Position eröffnet wurde, eine weitere Zufallszahl für den Verkaufs-Zweig generieren. Wenn `UseSell` aktiviert ist und das Ergebnis 0.5 übersteigt, eine Verkaufsmarktorder senden.
5. Nach einem Einstieg optionale Stop-Loss- und Take-Profit-Orders `StopLossTakeProfitPips` MetaTrader-Pips vom Kerzenschluss entfernt anhängen. Schutzorders werden automatisch storniert, wenn die Position auf null zurückkehrt.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-Minuten-Zeitrahmen | Primärer Zeitrahmen der Strategie. |
| `TradeVolume` | `decimal` | `1` | Ordergröße für jeden Markteinstieg. |
| `StopLossTakeProfitPips` | `int` | `100` | Abstand (in MetaTrader-Pips) zwischen Einstiegspreis und beiden Schutzorders. Auf `0` setzen, um Stop-Loss und Take-Profit zu deaktivieren. |
| `UseBuy` | `bool` | `true` | Zufällige Long-Einstiege aktivieren. |
| `UseSell` | `bool` | `true` | Zufällige Short-Einstiege aktivieren. |

## Unterschiede zum ursprünglichen MetaTrader-Experten
- MetaTrader gibt die Freeze- und Stop-Level des Instruments preis; der StockSharp-Port emuliert nur die Pip-Distanz-Konvertierung und verlässt sich auf den Broker, ungültige Stop-Abstände ggf. abzulehnen.
- Der ursprüngliche EA verwendet die aktuellen Bid/Ask-Kurse. Die StockSharp-Strategie basiert Schutzorders auf dem Kerzenschlusskurs, weil die High-Level-API aggregierte Kerzendaten empfängt; Slippage und Spread-Effekte müssen extern behandelt werden.
- MetaTrader arbeitet mit einzelnen Tickets, während StockSharp Net-Positionen verwaltet. Die Konvertierung hält höchstens eine Net-Position und entfernt Schutzorders, sobald die Exposure auf null zurückgeht.

## Verwendungshinweise
- Wählen Sie ein realistisches `TradeVolume`, das dem Lot-Schritt des Wertpapiers entspricht. Der Konstruktor wendet diesen Wert auch auf `Strategy.Volume` an, damit Hilfsmethoden Orders mit der beabsichtigten Menge senden.
- Passen Sie `StopLossTakeProfitPips` an die Volatilität des Instruments an. Die Strategie multipliziert Pips mit dem Preisschritt des Wertpapiers (skaliert für 3/5-stellige Kurse), um eine native Preisdistanz zu erhalten.
- Aktivieren Sie nur `UseBuy` oder `UseSell`, wenn Sie möchten, dass der Zufallsgenerator Trades nur in einer Richtung öffnet.
- Da Einstiege zufällig sind, überwachen Sie die Strategie mit zusätzlichen Risikolimits oder einer maximalen Positionsdauer, wenn Sie deterministische Ausstiegsbedingungen benötigen.

## Indikatoren
- Keine. Die Strategie beruht ausschließlich auf zufälliger Trade-Generierung und optionalen Schutzorders.
