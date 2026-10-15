# Strategie zum Trading mit qualifiziertem RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie reproduziert den MetaTrader-Expertenberater "Trade on qualified RSI" mit der High-Level API von StockSharp. Sie verhält sich als konträres System: Sie interpretiert verlängerte RSI-Messwerte als Erschöpfung und eröffnet eine Position gegen die vorherrschende Bewegung, nachdem der Momentum für mehrere Kerzen anhält. Trailing-Stops werden in Preisschritten verwaltet, sodass der Stop der Position nur folgt, wenn sich der Preis zugunsten der Position bewegt.

## Signallogik
### Indikator
* Relativer Stärke-Index mit einem konfigurierbaren Zeitraum (Standard: 28).
* Berechnet auf dem ausgewählten Kerzen-Abonnement (Standard: 15-Minuten-Kerzen).

### Short-Einstieg
1. Die letzte geschlossene Kerze hat einen RSI größer oder gleich dem oberen Schwellenwert (Standard: 55).
2. Jede der vorherigen `CountBars` geschlossenen Kerzen hatte ebenfalls einen RSI über demselben Schwellenwert. Intern zählt die Strategie aufeinanderfolgende Bars; das Signal wird ausgelöst, wenn der Zähler `CountBars + 1` erreicht.
3. Es ist keine aktive Position offen. Wenn ausgelöst, verkauft die Strategie zum Marktpreis mit dem konfigurierten Volumen und speichert den Kerzenschluss als Einstiegspreis.

### Long-Einstieg
1. Die letzte geschlossene Kerze hat einen RSI kleiner oder gleich dem unteren Schwellenwert (Standard: 45).
2. Jede der vorherigen `CountBars` geschlossenen Kerzen hatte ebenfalls einen RSI unter demselben Schwellenwert (`CountBars + 1` aufeinanderfolgende Messungen sind erforderlich).
3. Es gibt keine offene Position. Wenn ausgelöst, kauft die Strategie zum Marktpreis mit dem konfigurierten Volumen und notiert den Einstiegspreis.

## Positionsverwaltung
* **Anfangs-Stop:** Direkt nach dem Einstieg wird der Stop-Preis `StopLossPoints` Preisschritte vom Eintrittskurs entfernt platziert (unterhalb für Longs, oberhalb für Shorts). Preisschritte werden von `Security.PriceStep` ermittelt; wenn das Wertpapier es nicht definiert, fällt die Strategie auf `1` zurück.
* **Trailing:** Bei jeder abgeschlossenen Kerze wird der Stop in Richtung des aktuellen Schlusskurses gestrafft. Für Long-Positionen wird der Stop zu `Schluss - StopLossPoints * PriceStep`, wenn dieser Wert über dem vorherigen Stop liegt. Für Short-Positionen wird der Stop zu `Schluss + StopLossPoints * PriceStep`, wenn dieser Wert unter dem vorherigen Stop liegt.
* **Ausstieg:** Wenn das Kerzentief den Stop bei einer Long-Position unterschreitet oder das Kerzenhoch den Stop bei einer Short-Position überschreitet, schließt die Strategie die gesamte Position zum Marktpreis. Es gibt keine zusätzlichen Gewinnziele oder Umkehrsignale; neue Einstiege erfolgen nur nach dem Schließen der vorherigen Position.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| `RsiPeriod` | Rückblicklänge für den RSI-Indikator. | 28 |
| `UpperThreshold` | RSI-Level, das ein Short-Setup qualifiziert. | 55 |
| `LowerThreshold` | RSI-Level, das ein Long-Setup qualifiziert. | 45 |
| `CountBars` | Wie viele vorherige Bars über dem Schwellenwert bleiben müssen (`CountBars + 1` aufeinanderfolgende Bars insgesamt). | 5 |
| `StopLossPoints` | Stop-Abstand ausgedrückt in Preisschritten. Der tatsächliche Preisoffset entspricht `StopLossPoints * PriceStep`. | 21 |
| `TradeVolume` | Mit jeder Einstiegsorder gesendetes Volumen. | 1 |
| `CandleType` | Für Indikatorberechnungen verwendetes Kerzen-Abonnement. | 15-Minuten-Kerzen |

Alle Parameter können optimiert werden. Die Schwellenwerte erlauben Dezimalwerte, sodass eine feinkörnige Abstimmung der RSI-Grenzen möglich ist.

## Implementierungshinweise
* Die Strategie verwendet `SubscribeCandles(...).Bind(...)`, um den RSI-Indikator zu speisen und nur zu reagieren, wenn die Kerze vollständig ausgebildet ist.
* RSI-Werte werden nicht per Index vom Indikator zurückgelesen; stattdessen verfolgen Zähler, wie viele aufeinanderfolgende abgeschlossene Kerzen die Schwellenwerte respektieren.
* Schutz-Stops werden innerhalb der Strategie simuliert. Orders werden zum Marktpreis geschlossen, wenn das Stop-Level überschritten wird, anstatt separate Stop-Orders zu platzieren.
* Protokollmeldungen werden für Ein- und Ausstiege erzeugt, was die ausführliche Ausgabe des ursprünglichen Expertenberaters spiegelt.

## Verwendung
1. Fügen Sie die Strategie einer StockSharp-Anwendung hinzu, weisen Sie das gewünschte Wertpapier und Portfolio zu und konfigurieren Sie die Kerzenserie.
2. Passen Sie die RSI-Schwellenwerte, die Anzahl der qualifizierenden Bars und den Stop-Abstand an die Volatilität des Zielinstruments an.
3. Starten Sie die Strategie. Überwachen Sie das Protokoll, um zu sehen, wann Signale auftreten und wie sich der Trailing-Stop entwickelt.
4. Erwägen Sie, den integrierten Optimierer auszuführen, um bessere Kombinationen von Schwellenwerten oder Stop-Abständen für spezifische Märkte zu suchen.
