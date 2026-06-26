# Breakdown Catcher-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Breakdown Catcher-Strategie ist ein Bar-für-Bar-Ausbruchssystem, das vom MetaTrader-Expert Advisor „Breakdown catcher" portiert wurde. Nach jeder abgeschlossenen Kerze platziert die Strategie virtuelle Ausbruchsniveaus oberhalb des vorherigen Hochs und unterhalb des vorherigen Tiefs (optional um einen Einzug verschoben). Wenn die nächste Kerze eines dieser Niveaus durchbricht, eröffnet die Strategie eine Position in der Ausbruchsrichtung und weist sofort Stop-Loss, Take-Profit und optionalen Trailing-Schutz in Pips zu.

## Handelslogik
1. Beim Schließen jeder Kerze werden das Hoch und das Tief der abgeschlossenen Kerze als Referenzbereich für die nächste Periode verwendet.
2. Kauf-Ausbruchsniveau = vorheriges Hoch + Einzug (in Pips). Verkaufs-Ausbruchsniveau = vorheriges Tief − Einzug.
3. Wenn die aktuelle Kerze das Kaufniveau durchbricht, während keine Position offen ist, eröffnet die Strategie eine Long-Position zum Markt, entfernt jeden Short-Kontext und speichert die Schutzniveaus.
4. Wenn die aktuelle Kerze das Verkaufsniveau durchbricht, während flat, eröffnet die Strategie eine Short-Position zum Markt.
5. Stop-Loss- und Take-Profit-Abstände werden von Pips in absolute Preise umgerechnet, indem der Instrument-Preisschritt und die klassische MetaTrader-Anpassung für 3/5-Dezimalstellen-Instrumente verwendet werden.
6. Ein Trailing Stop kann den Schutzpreis straffen, nachdem sich der Trade um mindestens `TrailingStop + TrailingStep` Pips in die günstige Richtung bewegt hat. Der Trailing-Schritt imitiert die MetaTrader-Logik, bei der sich der Stop nur nach einer ausreichenden zusätzlichen Bewegung bewegt.
7. Wenn beide Ausbruchsniveaus innerhalb derselben Kerze erreicht werden, überspringt die Strategie den Handel für diesen Balken, um eine mehrdeutige Ausführungsreihenfolge zu vermeiden.
8. Ein Spread-Filter blockiert neue Einträge, wenn der aktuelle Bid-Ask-Spread die konfigurierte `AllowedSpreadPoints`-Zahl übersteigt.

## Geldverwaltung
* Die Strategie verwendet das Basis-`Strategy.Volume` für die Ordergröße. Beim Umkehren von Positionen wird das Volumen um den absoluten Wert der aktuellen Position erhöht, um einen vollständigen Flip zu gewährleisten.
* Stop-Loss, Take-Profit und Trailing Stops werden intern durch Ausgabe von Market-Exit-Orders verwaltet, wenn Preisbereiche die Schutzniveaus umfassen.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `StopLossPips` | Stop-Loss-Abstand in Pips. | `30` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. | `90` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. Auf `0` setzen, um Trailing zu deaktivieren. | `30` |
| `TrailingStepPips` | Zusätzlicher Fortschritt, der erforderlich ist, bevor sich der Trailing Stop bewegt. Muss positiv sein, wenn Trailing aktiviert ist. | `5` |
| `IndentPips` | Zusätzlicher Versatz, der auf die Ausbruchsniveaus angewendet wird. | `0` |
| `AllowedSpreadPoints` | Maximaler Spread gemessen in Rohpunkten (`PriceStep`-Einheiten). | `5` |
| `CandleType` | Kerzenserie für die Ausbruchserkennung. | `1h Zeitrahmen` |

## Hinweise und Einschränkungen
* Die Pip-Konvertierung folgt derselben Ziffernanpassung wie der Original-EA: Wenn das Instrument 3 oder 5 Dezimalstellen hat, entspricht ein Pip zehn Preisschritten.
* Da die High-Level-API von StockSharp mit Kerzenereignissen arbeitet, kann die genaue Reihenfolge, in der beide Ausbruchsniveaus innerhalb einer einzelnen Kerze getroffen werden, nicht bestimmt werden; daher überspringt die Strategie solche Balken.
* Schutzorders werden mit Market-Exits modelliert, sodass die Strategie eigenständig ist und nicht auf broker-seitige Stop-Orders angewiesen ist.
