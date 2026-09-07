# Überwachung des Equity-Drawdowns
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm erstellt aus dem Strategie-P&L eine Equity-Kurve, speichert ein dauerhaftes Hoch, protokolliert jede neue Überschreitung der Drawdown-Schwelle genau einmal und führt einen bewusst seltenen, abgesicherten Long-Zyklus aus, damit sich die überwachten Kontowerte im Backtest bewegen.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen für BTCUSDT bilden den einzigen Abtasttakt. Ein paralleles Level-1-Abonnement des besten Geldkurses hält die Bewertung des nicht realisierten P&L zwischen den Kerzenabtastungen aktuell.
- P&L change aktualisiert stille Speicher für realisierten und nicht realisierten P&L in seinem eigenen Ereignistakt. Jede abgeschlossene Kerze gibt beide letzten Werte zusammen mit Start Balance genau einmal an `Equity = Start Balance + Realized P&L + Unrealized P&L` weiter.
- `max(gespeichertes Hoch, Equity)` erhält das Equity-Hoch des gesamten Laufs. Highest(2) gibt nur gebildete Werte aus, bestätigt diesen monotonen Hoch-Stream und fügt eine Abtastung zum Aufwärmen hinzu, ohne die Hoch-Historie zu verkürzen.
- Der Drawdown lautet `(Peak - Equity) / Peak * 100`. Comparison prüft `Drawdown >= Drawdown Alert`; Crossing und ein boolescher Speicher geben nur eine neue aufwärts gerichtete Schwellenkreuzung an das Protokoll weiter.
- Modify position eröffnet bei leerer Position eine Long-Position zum Marktpreis. Der Schutz mit absoluten Abständen schließt sie, und ein Timer über 1.440 Kerzen erlaubt den nächsten Einstieg erst nach fünf Tagen nachfolgender Fünf-Minuten-Kerzen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Nachdem Highest(2) gebildet ist, gibt ein verfügbares Einstiegs-Flag Volume 1 an einen Modify-position-Block mit Buy, OpenPosition und MarketOrder weiter. Der erste Einstiegsversuch erfolgt daher bei der zweiten abgeschlossenen Kerze.
- **Short-Einstieg**: Das Diagramm eröffnet keine Short-Positionen. Sell-Ausführungen sind schützende Ausstiege aus der Long-Position.
- **Ausstieg**: Position protection sendet nach einer günstigen Bewegung von 0.04 oder einer ungünstigen Bewegung von 0.03 absoluten Preiseinheiten einen Marktausstieg. Die Einstiegsausführung startet die Abkühlung über 1.440 nachfolgende Kerzen; der Timer und nicht die Ausstiegsausführung setzt die Einstiegsbereitschaft zurück.

## Parameter

| Parameter | Standardwert | Beschreibung |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Instrument für die Abonnements Candles, Level 1 und Strategy trades. Setzen Sie Strategy Security für Transaktionen und P&L auf dasselbe Instrument. |
| Candle Series | 00:05:00 | Intervall abgeschlossener Kerzen und Takt für Equity-Abtastungen sowie Abkühlschritte. |
| Start Balance | 1000 | Basisbetrag, der bei der Equity-Berechnung zum realisierten und nicht realisierten P&L addiert wird. |
| Peak Confirmation Length | 2 | Highest-Länge über dem monotonen, dauerhaften Hoch-Stream; der Handel beginnt erst mit der zweiten Abtastung. |
| Drawdown Alert, % | 1 | Ein Protokolleintrag wird geschrieben, wenn der Drawdown diesen Wert von unten erreicht oder überschreitet. |
| Volume | 1 | Menge jedes Long-Einstiegs. |
| Entry Cooldown N | 1440 | Anzahl nachfolgender abgeschlossener Fünf-Minuten-Kerzen zwischen erlaubten Einstiegen, entsprechend fünf Tagen. |
| Take Distance | 0.04 | Günstiger absoluter Preisabstand, der die Long-Position zum Marktpreis schließt. |
| Stop Distance | 0.03 | Ungünstiger absoluter Preisabstand, der die Long-Position zum Marktpreis schließt. |

## Diagrammdetails

- Die BTC-[Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) versorgt abgeschlossene [Candles](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html), den besten Geldkurs aus [Level 1](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html) und [Strategy trades](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html). Die Handelsblöcke nutzen Strategy Security und Strategy Portfolio.
- Stille Variable-Speicher trennen den ereignisgesteuerten [P&L change](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html)-Stream vom Kerzentakt. Ihre feste Ausgabereihenfolge liefert jeder [Formula](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html) einen vollständigen Eingabesatz derselben Kerze.
- Das dauerhafte Hoch beginnt bei null, sodass Änderungen von Start Balance gültig bleiben. Die max-Formula aktualisiert diesen Zustand, bevor ihre monotone Ausgabe in den gebildeten [Indicator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) Highest(2) gelangt.
- [Comparison](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) und [Crossing](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) erhalten Schwelle und Drawdown in fester Reihenfolge. Eine false-Kreuzung bei der Erholung wird ignoriert; eine true-Aufwärtskreuzung gibt den gespeicherten Prozentsatz über [String Formatter](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) an eine Log-[Notification](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) weiter.
- Der Abkühlblock [Delay](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) verarbeitet jede Kerze vor allen Entscheidungen derselben Kerze. Eine Buy-Ausführung aktiviert N = 1440; die Ausgabe setzt das Einstiegs-Flag zurück, bevor die berechtigte Kerze Highest erreicht.
- Die Buy-Ausführung von [Modify position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) initialisiert die lokale [Position protection](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). Der Chart zeigt Kerzen, abgetastete Equity, Hoch, Drawdown, beide P&L-Komponenten, Einstiegsausführungen, Schutzausführungen und alle Strategieausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, setzen Sie Strategy Security auf BTCUSDT@BNBFT und führen Sie das Diagramm mit den mitgelieferten März-Historiendaten aus. Prüfen Sie vor dem Live-Handel Equity-Skalierung, absolute Schutzabstände, Warnprozentsatz und fünftägige Abkühlung für Ihr Instrument.
