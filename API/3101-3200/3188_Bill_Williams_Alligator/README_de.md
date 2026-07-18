# Bill Williams Alligator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader 5 Expert Advisor **„Bill Williams.mq5"** von Vladimir Karputov auf die High-Level-API von StockSharp. Sie abonniert eine einzelne Kerzenserie, rekonstruiert Bill-Williams-Fraktalpunkte und bewertet Ausbrüche relativ zu den verschobenen Alligator-Linien. Wenn die aktuelle Kerze jenseits des nächsten Auf- oder Ab-Fraktals schließt und dieser Fraktal außerhalb aller drei Alligator-Kurven (Kiefer, Zähne, Lippen) liegt, öffnet das System eine Position. Optionale Geldverwaltungsfunktionen reproduzieren die originalen Inputs wie Stop-Loss, Take-Profit, Trailing Stop, Signalumkehr und automatisches Schließen entgegengesetzter Positionen.

## Handelslogik

1. **Fraktalerkennung** – jede abgeschlossene Kerze aktualisiert rollende Puffer von Hochs und Tiefs. Der Algorithmus scannt bis zu `FractalsLookback` abgeschlossene Balken und findet die neuesten bestätigten Auf- und Ab-Bill-Williams-Fraktale (Fünfbalken-Muster).
2. **Alligator-Rekonstruktion** – der Medianpreis `(High + Low) / 2` speist drei `SmoothedMovingAverage`-Instanzen, die den Kiefer, die Zähne und die Lippen darstellen. Ihre Werte werden um die konfigurierte Anzahl von Balken nach vorne verschoben, um den MetaTrader-Darstellungsregeln zu entsprechen.
3. **Ausbruchsvalidierung** – ein Long-Setup erfordert, dass der neueste Aufwärtsfraktal über dem verschobenen Kiefer, den Zähnen und den Lippen bleibt, während die neueste Kerze über dem Fraktalpreis schließt. Ein Short-Setup spiegelt die Logik unterhalb des Alligators.
4. **Orderausführung** – standardmäßig eröffnet die Strategie eine einzelne Market Order mit `OrderVolume`, wenn der Ausbruch erkannt wird und keine Position gehalten wird. Wenn `CloseOppositePositions` aktiviert ist, wird eine entgegengesetzte Position geschlossen, bevor eine neue eröffnet wird. Das Setzen von `ReverseSignals = true` tauscht die Ausbruchseiten aus, um den Rückwärtsmodus des EA zu reproduzieren.
5. **Risikomanagement** – konfigurierbare Stop-Loss- und Take-Profit-Niveaus werden intern gespeichert und bei jeder Kerze bewertet. Der Trailing Stop aktiviert sich, sobald sich der Markt um `TrailingStopPips + TrailingStepPips` in der Trade-Richtung bewegt und schreitet fort, wenn der Preis voranschreitet. Stops werden in „Pips" ausgedrückt, abgeleitet vom `PriceStep` des Instruments, einschließlich der MetaTrader-Anpassung für 3- oder 5-Stellen.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `OrderVolume` | Handelsgröße in Lots oder Kontrakten für Market-Einträge. | `0.1` |
| `StopLossPips` | Anfänglicher Stop-Loss-Abstand in Pips. Auf `0` setzen zum Deaktivieren. | `50` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Auf `0` setzen zum Deaktivieren. | `50` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. `0` deaktiviert die Trailing-Logik. | `10` |
| `TrailingStepPips` | Zusätzlicher Pip-Gewinn, der erforderlich ist, bevor sich der Trailing Stop erneut bewegt. Muss positiv sein, wenn Trailing aktiviert ist. | `5` |
| `JawPeriod` | Länge der geglätteten Moving Average für den Alligator-Kiefer (blau). | `13` |
| `JawShift` | Vorwärtsverschiebung für die Kieferwerte, gemessen in Balken. | `8` |
| `TeethPeriod` | Länge der geglätteten Moving Average der Zähne (rot). | `8` |
| `TeethShift` | Vorwärtsverschiebung für die Zahnwerte. | `5` |
| `LipsPeriod` | Länge der geglätteten Moving Average der Lippen (grün). | `5` |
| `LipsShift` | Vorwärtsverschiebung für die Lippenwerte. | `3` |
| `FractalsLookback` | Anzahl der abgeschlossenen Kerzen, die bei der Suche nach den neuesten bestätigten Fraktalen gescannt werden. | `100` |
| `ReverseSignals` | Wenn `true`, kommen Kaufsignale von Ab-Fraktal-Ausbrüchen und Verkaufssignale von Auf-Fraktal-Ausbrüchen. | `false` |
| `CloseOppositePositions` | Wenn `true`, schließt die Strategie eine bestehende entgegengesetzte Position, bevor ein neuer Trade eingegangen wird. | `false` |
| `CandleType` | Kerzenserie für Berechnungen und Signale. | `TimeFrame(1h)` |

## Hinweise

- Die Strategie operiert ausschließlich auf **abgeschlossenen Kerzen** und ignoriert Intrabar-Ticks, womit der Bar-für-Bar-Workflow des Original Expert Advisors nachgebildet wird.
- Um die MetaTrader 5 Pip-Berechnung zu emulieren, multipliziert die Strategie den Exchange-`PriceStep` um 10, wenn das Instrument 3 oder 5 Dezimalstellen hat.
- Schutzorders und der Trailing Stop werden intern verwaltet. Wenn eine Stop- oder Zielbedingung innerhalb der nächsten Kerze erfüllt ist, wird die Position zum Markt geschlossen, um das Order-Management des EA nachzuahmen.
- Die Alligator-Indikatoren werden automatisch gezeichnet, wenn ein Diagrammbereich verfügbar ist, was einen visuellen Vergleich zwischen dem StockSharp-Port und der MetaTrader-Vorlage ermöglicht.
- Python- und Testprojekte werden gemäß den Repository-Richtlinien für neue Konvertierungen absichtlich weggelassen.
