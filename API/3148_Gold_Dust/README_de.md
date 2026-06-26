# Gold Dust-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Gold Dust-Strategie reproduziert den MetaTrader 5-Expertenberater "Gold Dust" innerhalb des StockSharp-Frameworks. Sie wertet bis zu zwei Perceptrons aus, die aus einem linearen gewichteten gleitenden Durchschnitt (LWMA) auf dem gewichteten Kerzenpreis aufgebaut sind. Jedes Perceptron beobachtet, wie der Preis vom gleitenden Durchschnitt an vier verschiedenen Rückblickpunkten, getrennt durch den MA-Zeitraum, abweicht. Wenn die Perceptron-Ausgabe positiv ist, öffnet der originale Experte eine Verkaufsposition, und wenn sie negativ ist, öffnet er eine Kaufposition. Der StockSharp-Port behält dasselbe Verhalten bei, während er sich auf die High-Level-Kerzen-API stützt.

## Signalgenerierung

1. Den konfigurierten `CandleType` abonnieren und einen `WeightedMovingAverage` mit dem Zeitraum aus `MaPeriod` berechnen.
2. Bei jeder fertigen Kerze die Eröffnungs- und Schlusspreise der Kerze zusammen mit dem LWMA-Wert speichern. Die Strategie hält immer drei vollständige MA-Perioden der Geschichte, um die `CopyRates`/`CopyBuffer`-Aufrufe aus der MQL-Version zu replizieren.
3. Preis/MA-Offsets berechnen:
   - `a1` – aktueller Schluss minus aktuellem LWMA
   - `a2` – Eröffnungspreis eine MA-Periode zurück minus LWMA an derselben Kerze
   - `a3` – Eröffnungspreis zwei MA-Perioden zurück minus LWMA an derselben Kerze
   - `a4` – Eröffnungspreis drei MA-Perioden zurück minus LWMA an derselben Kerze
4. Perceptron-Ausgabe berechnen `result = Σ (wi × ai)`, wobei jedes Gewicht der rohe Parameter (z.B. `X11`) minus 100 ist, entsprechend der originalen `w = x - 100`-Transformation.
5. Perceptron-Ausgaben interpretieren abhängig von `PassMode`:
   - `1` – nur das erste Perceptron verwenden.
   - `2` – nur das zweite Perceptron verwenden.
   - `3` – beide Perceptrons müssen dasselbe Vorzeichen ungleich null erzeugen.
6. Ein negatives Signal öffnet oder hält eine Long-Position, ein positives Signal öffnet oder hält eine Short-Position, und ein Nullsignal löst Gewinnmitnahmen auf bestehenden Positionen aus.

## Positionsverwaltung

- **Einstiege** – die Strategie handelt mit einem festen `TradeVolume`. Long einzugehen schließt jede ausstehende Short-Exposition und umgekehrt, sodass nur eine direktionale Position verbleibt.
- **Stop-Loss** – `StopLossPips` wird in einen absoluten Preisabstand unter Verwendung von `Security.PriceStep` umgerechnet. Für Instrumente mit drei oder fünf Dezimalstellen wird die Distanz mit zehn multipliziert, um die "angepasster Punkt"-Logik in der MQL-Version zu imitieren. Der Stop wird bei jeder abgeschlossenen Kerze ausgewertet.
- **Trailing Stop** – wenn `TrailingStopPips` größer als null ist, wird die Trailing-Logik aktiv. Nachdem sich der Preis um `TrailingStopPips + TrailingStepPips` zugunsten des Trades bewegt hat, wird der Stop auf `Schluss ± TrailingStopPips` gesetzt (je nach Richtung).
- **Gewinnverwaltung** – wenn kein Perceptron einer Richtung zustimmt (`signal == 0`), schließt die Strategie die Position nur, wenn der schwebende Gewinn positiv ist.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `TradeVolume` | `1` | Basisvolumen für jeden neuen Einstieg. Entgegengesetzte Positionen werden vor der neuen Seite abgeflacht. |
| `StopLossPips` | `150` | Anfänglicher Stop-Loss-Abstand in angepassten Pips (berücksichtigt den 3/5-Dezimalstellen-Multiplikator). Auf null setzen, um den anfänglichen Stop zu deaktivieren. |
| `TrailingStopPips` | `25` | Trailing-Stop-Abstand in angepassten Pips. Auf null setzen, um Trailing zu deaktivieren. |
| `TrailingStepPips` | `5` | Zusätzliche günstige Bewegung (in Pips) erforderlich, bevor der Trailing Stop vorrückt. |
| `MaPeriod` | `20` | Periodenlänge des gewichteten gleitenden Durchschnitts, der die Perceptrons speist. |
| `CandleType` | `H1` | Kerzenreihe für die Signalauswertung. Jeder andere vom Datenanbieter unterstützte Zeitrahmen kann gewählt werden. |
| `PassMode` | `1` | Steuert, welche Perceptron(s) ausgewertet werden: 1 – erstes, 2 – zweites, 3 – Konsens beider. |
| `X11`, `X21`, `X31`, `X41` | `100` | Rohgewichte für Perceptron #1. Die Strategie subtrahiert 100 von jedem Wert vor der Verwendung. |
| `X12`, `X22`, `X32`, `X42` | `100` | Rohgewichte für Perceptron #2, genauso wie das erste Set behandelt. |

## Hinweise zur Konvertierung

- Der originale EA stützte sich auf Tick-für-Tick-Updates zur Stop-Verwaltung; der StockSharp-Port wertet Stops und Trailing beim Kerzenschluss aus.
- Geldverwaltung über `CMoneyFixedMargin` wurde durch einen festen `TradeVolume`-Parameter ersetzt.
- Perceptron-Berechnungen vermeiden direkte Indikatorpuffer (`CopyBuffer`) durch Caching der notwendigen Kerzen- und MA-Werte in begrenzten Listen.
- Alle Pip-Distanzen respektieren die MetaTrader "angepasster Punkt"-Konvention: Bei 3 oder 5 Dezimalstellen wird die Distanz mit zehn multipliziert.

## Nutzungstipps

1. Ein Symbol erstellen oder auswählen, dann `CandleType` auf den Zeitrahmen setzen, der dem historischen Chart in der MQL-Version entspricht.
2. Die Perceptron-Gewichte (`X**`) und `PassMode` überprüfen, um mit der optimierten Konfiguration aus MetaTrader übereinzustimmen.
3. `TradeVolume` anpassen, damit es den Mindestgröße- und Schrittanforderungen des verbundenen Brokers entspricht.
4. Das Log überwachen: Jedes Mal, wenn der Trailing Stop vorrückt oder ein Stop-Loss ausgelöst wird, wird eine Meldung aufgezeichnet.
