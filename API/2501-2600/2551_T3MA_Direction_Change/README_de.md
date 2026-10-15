# T3 MA Richtungswechsel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie reproduziert das Verhalten des originalen **T3MA(barabashkakvn's edition)** Expertenberaters. Der Expertenberater nutzt den Indikator "T3MA-ALARM", der exponentielles Glätten zweimal anwendet und ein Signal gibt, wenn die geglättete Linie die Richtung wechselt. Der StockSharp-Port behält dasselbe Konzept: Er erstellt einen doppelt geglätteten exponentiellen gleitenden Durchschnitt (EMA der EMA) und handelt, wenn die Steigung dieser Kurve von fallend zu steigend oder umgekehrt wechselt.

Die Strategie arbeitet ausschließlich mit abgeschlossenen Kerzen. Signale können um eine konfigurierbare Anzahl von Bars verzögert werden, um die ursprüngliche `InpBarNumber`-Option nachzuahmen (Standard-Verzögerung: eine Bar). Orders werden per Marktausführung platziert, sodass das Portfolio zwischen Long- und Short-Exposition wechselt, ohne mehrere gleichzeitige gehedgte Positionen aufzubauen.

## Handelsregeln
1. Die konfigurierte Kerzenserie abonnieren und eine EMA der Schlusskurse berechnen. Eine zweite EMA über die Ausgabe der ersten EMA ausführen, wodurch die vom Indikator verwendete geglättete Reihe erzeugt wird.
2. Den aktuellen Wert der geglätteten Reihe (optional um `EMA Shift` Bars nach vorne verschoben) mit dem vorherigen Wert vergleichen. Die Steigung gilt als bullisch, wenn die Reihe steigt, und als bearisch, wenn sie fällt.
3. Wenn die Steigung von bearisch zu bullisch wechselt, ein **Kauf**-Signal in die Warteschlange stellen. Wenn es von bullisch zu bearisch wechselt, ein **Verkauf**-Signal einreihen. Neutrale Kerzen schieben ein Null-Signal in die Warteschlange, damit der Verzögerungszähler korrekt bleibt.
4. Nachdem die konfigurierte Anzahl abgeschlossener Kerzen (`Signal Delay`) verstrichen ist, das Warteschlangensignal ausführen. Ein verzögerter Kauf schließt jede offene Short-Position und geht Long mit dem Basis-`Trade Volume`. Ebenso schließt ein verzögerter Verkauf eine Long-Position und geht Short.
5. Schutzstop-Loss- und Take-Profit-Orders werden über `StartProtection` initialisiert. Beide Abstände werden in Kursschritten ausgedrückt, sodass sie sich automatisch an die Tick-Größe des ausgewählten Instruments anpassen.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `EMA Length` | Länge der EMA für beide Glättungsläufe. Entspricht dem `MAPeriod`-Input in der MetaTrader-Implementierung. |
| `EMA Shift` | Anzahl der Bars, um die die geglättete EMA vor dem Steigungsvergleich verschoben wird. Entspricht dem `MAShift` des Indikators. |
| `Signal Delay` | Anzahl abgeschlossener Kerzen, die vor der Signalausführung gewartet wird. Spiegelt `InpBarNumber` wider, sodass ein Wert von 1 das Signal der vorherigen Bar handelt. |
| `Stop Loss (steps)` | Stop-Loss-Abstand in Kursschritten. Auf null setzen zum Deaktivieren. |
| `Take Profit (steps)` | Take-Profit-Abstand in Kursschritten. Auf null setzen zum Deaktivieren. |
| `Trade Volume` | Basisordergröße für neue Einstiege. Bei einer Positionsumkehr addiert die Strategie die absolute aktuelle Positionsgröße zu diesem Wert. |
| `Candle Type` | Kerzendatentyp für Berechnungen (Standard: 5-Minuten-Zeitrahmen). |

## Risikomanagement
* `StartProtection` registriert automatisch Stop-Loss- und Take-Profit-Niveaus beim Strategiestart. Beide Niveaus folgen der Tick-Größe des Instruments und bleiben für die gesamte Lebensdauer der Strategie aktiv.
* Positionsumkehrungen werden mit Marktorders ausgeführt. Wenn die Signalrichtung mit der aktuellen Exposition übereinstimmt, werden keine zusätzlichen Trades ausgelöst, was ungewolltes Pyramidisieren verhindert.
* Bei jedem Trade werden Logs ausgegeben, um den Grund und den Referenzpreis aus der Quellkerze nachzuverfolgen.

## Unterschiede zur MQL5-Version
* MetaTrader 5 benötigte ein Hedging-Konto und konnte mehrere Positionen akkumulieren. Die StockSharp-Version hält eine einzelne Nettoposition und kehrt sie um, wenn das entgegengesetzte Signal ausgelöst wird.
* Die Signalverarbeitung ist kerzenbasiert und erfolgt einmal pro abgeschlossener Kerze statt bei jedem Tick, was innerhalb der High-Level-API von StockSharp natürlicher ist.
* Die Stop-Loss- und Take-Profit-Verwaltung wird über `StartProtection` gehandhabt, anstatt manuell SL/TP-Preise mit jeder Order zu übermitteln.
* Englische Kommentare, strukturierte Parameter und Chart-Hilfsmittel wurden für bessere Lesbarkeit in der StockSharp-Umgebung hinzugefügt.

## Verwendungshinweise
1. Die Strategie an das gewünschte Wertpapier anhängen und sicherstellen, dass der Kerzentyp dem Zeitrahmen entspricht, der bei der Optimierung des ursprünglichen Expertenberaters verwendet wurde.
2. `EMA Length` und die Risikoparameter an die Instrumentenvolatilität anpassen. Höhere Verzögerungen (`Signal Delay`) verlangsamen Reaktionen und können Rauschen filtern.
3. Da die Strategie mit Kursschritten arbeitet, sicherstellen, dass die `PriceStep`-Eigenschaft des Wertpapiers korrekt konfiguriert ist, damit Schutzorders in sinnvollen Abständen platziert werden.
