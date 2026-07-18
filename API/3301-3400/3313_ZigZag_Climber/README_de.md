# ZigZag-Climber-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der von fxDreema erzeugte ZigZag-Climber-Expert-Advisor enthält nur drei Blöcke: einen **No trade**-Filter, gefolgt von **Buy now**- und **Sell now**-Aktionen. Sobald das Terminal erkennt, dass keine Positionen offen sind, sendet es sofort eine Markt-Kauforder mit vordefinierten Stop-Loss- und Take-Profit-Niveaus und platziert ohne weitere Prüfungen eine symmetrische Markt-Verkaufsorder. Beide Trades übernehmen dieselben Risikoparameter und sollen als gehedgtes Paar koexistieren.

Dieser C#-Port reproduziert dieses Verhalten in StockSharp, indem er auf die erste abgeschlossene Kerze des gewählten Zeitrahmens wartet und dann Kauf- und Verkaufsschenkel direkt nacheinander mit identischen Schutzdistanzen sendet. Zusätzliche Signalgenerierung, Trailing-Logik oder Positionsverwaltung gibt es nicht, exakt wie im MQL-Quellprojekt.

## Handelslogik
1. Warten, bis die erste Kerze des konfigurierten Zeitrahmens vollständig gebildet ist.
2. Wenn die Strategie handeln darf und noch keine Orders platziert wurden, eine Markt-**Kauforder** mit festem Volumen senden.
3. Stop-Loss- und Take-Profit-Orders an den Long-Trade mit MetaTrader-artigen Pip-Distanzen anhängen (über `PriceStep` des Instruments konvertiert).
4. Sofort eine Markt-**Verkaufsorder** mit demselben Volumen senden und gespiegelte Schutzniveaus anhängen.
5. Für den Rest des Laufs keine weiteren Orders eröffnen.

> **Wichtig:** MetaTrader 4 arbeitet im Hedging-Modus, daher können beide Seiten gleichzeitig offen bleiben. StockSharp verwendet das Ausführungsmodell des Brokers; auf Netting-Konten wird die zweite Order die erste ausgleichen und die Strategie endet flach. Verwenden Sie einen hedgingfähigen Connector (z. B. MetaTrader-Gateway für Hedge-Konten), wenn beide Schenkel offen bleiben sollen.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `Candle Type` | 1 Minute | Zeitrahmen, der die einmalige Einstiegssequenz auslöst. |
| `Trade Volume` | 0.01 | Festes Volumen für beide Marktorders. |
| `Stop-Loss (pips)` | 99.9 | Distanz des Schutzstops in MetaTrader-Pips (behandelt 4-/5-stellige Symbole automatisch). |
| `Take-Profit (pips)` | 100 | Zieldistanz in MetaTrader-Pips. |

Alle Distanzen werden über `PriceStep` und Dezimalpräzision des Instruments in Preispunkte umgerechnet, bevor sie an `SetStopLoss`/`SetTakeProfit` übergeben werden.

## Risikomanagement
Die Strategie verlässt sich auf den integrierten Dienst `StartProtection()` und die Hilfsmethoden `SetStopLoss`/`SetTakeProfit`, um Schutzorders direkt nach jeder Marktorder zu platzieren. Es gibt keine Trailing- oder Break-even-Logik.

## Nutzungshinweise
- Weisen Sie vor dem Start die gewünschte Security und das Portfolio zu. Stellen Sie sicher, dass das Symbol `PriceStep` und `Decimals` bereitstellt, damit die Pip-Konvertierung korrekt funktioniert.
- Da die Einstiegslogik nur einmal läuft, ist ein Neustart der Strategie der einzige Weg, einen neuen Hedge-Zyklus zu erzeugen.
- Beim Test auf einem Netting-Simulator weicht das reale Verhalten von MetaTrader ab: Die Verkaufsorder neutralisiert die Kauforder nahezu sofort.
