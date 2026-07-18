# Eliot-Waves-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Eliot-Waves-Strategie repliziert das Verhalten des MetaTrader Expert Advisors "Eliot Waves" mit der High-Level-API von StockSharp. Der Algorithmus kombiniert Trenderkennung über zwei linear gewichtete gleitende Durchschnitte mit Momentum-Bestätigung und volatilitätsbasierten Ausstiegen. Alle Berechnungen werden auf abgeschlossenen Kerzen eines konfigurierbaren Zeitrahmens ausgeführt, um die deterministische Ausführung des ursprünglichen Roboters zu spiegeln.

## Handelslogik

1. **Trendfilter.** Die Strategie vergleicht eine schnelle LWMA (Standardperiode 6) mit einer langsamen LWMA (Standardperiode 85), berechnet über den typischen Kerzenpreis. Long-Trades werden nur betrachtet, wenn die schnelle LWMA über der langsamen LWMA schließt, während Short-Trades die entgegengesetzte Ausrichtung verlangen.
2. **Momentum-Bestätigung.** Ein Momentum-Indikator (Standardperiode 14) muss mindestens eine der letzten drei Messungen zeigen, die um mehr als den konfigurierten Schwellenwert (Standard 0.3) vom neutralen Wert 100 abweicht. Dies repliziert den ursprünglichen EA, der die absolute Differenz von drei jüngsten Momentum-Werten prüfte.
3. **Kerzenstrukturfilter.** Long-Signale verlangen, dass das Tief der Kerze vor zwei Bars unter dem Hoch der vorherigen Kerze lag. Short-Signale verlangen, dass das Tief der vorherigen Kerze unter dem Hoch vor zwei Bars bleibt. Dadurch wird der divergenzartige Filter aus dem Quellcode erfasst.
4. **Positionsskalierung.** Jedes Signal versucht, einen festen Volumenschritt (Standard 0.1) bis zur maximalen Anzahl von Schritten (Standard 10) hinzuzufügen. Die Strategie schließt jede Gegenexposure, bevor sie eine neue Position eröffnet, um mit der MetaTrader-Implementierung ausgerichtet zu bleiben.

## Risikomanagement

- **Stop-Loss und Take-Profit.** Preisziele werden in Pips relativ zum durchschnittlichen Einstiegspreis definiert und bei jeder Positionsänderung neu berechnet.
- **Trailing Stop.** Wenn aktiviert, wird der Stop hinter den Preis gezogen, sobald der offene Gewinn die Trailing-Distanz überschreitet.
- **Break-even.** Nach Erreichen des konfigurierten Triggers wird der Stop auf den Einstiegspreis plus optionalen Offset verschoben, um aufgelaufene Gewinne zu schützen.
- **Bollinger-Band-Ausstieg.** Long-Positionen steigen aus, wenn der Preis das untere Band eines 20-Perioden-Bollinger-Kanals berührt; Short-Positionen steigen beim Berühren des oberen Bands aus. Dies spiegelt die volatilitätsbasierte Schließlogik aus dem MQL-Skript.
- **MACD-Bestätigung.** Positionen werden auch bei einer MACD-(12, 26, 9)-Signalkreuzung gegen die Handelsrichtung geschlossen, wodurch der monatliche MACD-Ausstieg des ursprünglichen Expert Advisors reproduziert wird.
- **Erzwungener Ausstiegsschalter.** Der Parameter `EnableExitStrategy` erlaubt einem Operator, jede offene Position sofort zu liquidieren.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Volumen für jeden Positionsschritt. | 0.1 |
| `CandleType` | Kerzenzeitrahmen, der für alle Indikatoren verwendet wird. | 15-Minuten-Kerzen |
| `FastMaPeriod` / `SlowMaPeriod` | Perioden der schnellen und langsamen linear gewichteten gleitenden Durchschnitte. | 6 / 85 |
| `MomentumPeriod` | Momentum-Rückblick im Bestätigungsblock. | 14 |
| `MomentumThreshold` | Minimale Abweichung von 100, die zum Aktivieren des Handels erforderlich ist. | 0.3 |
| `StopLossPips` / `TakeProfitPips` | Stop-Loss- und Take-Profit-Distanzen in Pips. | 20 / 50 |
| `EnableTrailing` / `TrailingStopPips` | Schalter und Distanz für die Trailing-Stop-Funktion. | true / 40 |
| `EnableBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Break-even-Aktivierungsschalter, Trigger und Offset in Pips. | true, 30, 30 |
| `MaxPositions` | Maximal zulässige Anzahl von Volumenschritten. | 10 |
| `EnableExitStrategy` | Zwingt die Strategie, die Position zu glätten, wenn aktiviert. | false |

## Hinweise zur Umrechnung

- Die StockSharp-Implementierung nutzt die High-Level-Pipeline `SubscribeCandles().BindEx(...)`, um alle Indikatoren gleichzeitig zu verarbeiten und strikt auf abgeschlossenen Kerzen zu arbeiten.
- Die Pip-Umrechnung verwendet nach Möglichkeit den Preisschritt des Wertpapiers und fällt auf den Preisschrittwert zurück, wenn der Broker keine Pip-Präzision bereitstellt; das entspricht dem adaptiven Verhalten der MetaTrader-Version.
- Stop-Loss-, Take-Profit-, Trailing- und Break-even-Logik werden intern verwaltet, statt Brokerorders zu verwenden, damit das Verhalten in Backtests deterministisch bleibt.
- Alert-, E-Mail- und Benachrichtigungsaufrufe aus dem MQL-Expert wurden entfernt, da StockSharp eigene Logging-Funktionen bietet.

## Nutzungstipps

1. Wählen Sie das gewünschte Instrument und passen Sie `TradeVolume` sowie `MaxPositions` an die Kontogröße an. Die Standardwerte reproduzieren die konservative Skalierung des EA.
2. Optimieren Sie `MomentumThreshold`, `StopLossPips` und `TrailingStopPips` auf historischen Daten, wenn der Zielmarkt andere Volatilitätseigenschaften aufweist.
3. Stellen Sie beim Testen auf mehreren Symbolen sicher, dass das Wertpapier einen korrekten Preisschritt bereitstellt, damit pipbasierte Distanzen genau umgerechnet werden.
4. Überwachen Sie das Log auf die Warnung *"Unable to determine pip size from security settings"*. Wenn sie erscheint, konfigurieren Sie das Instrument mit dem korrekten Preisschritt, um falsch abgestimmte Risikoniveaus zu vermeiden.
