# ComFracti Fractal RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
ComFracti Fractal RSI Strategy ist eine StockSharp-Portierung des MetaTrader-Experten *ComFracti*. Der Algorithmus sucht mithilfe von Bill Williams-Fraktalen in zwei Zeitrahmen nach Richtungsverzerrungen und filtert die Signale mit einem schnellen RSI, der anhand täglicher Kerzen berechnet wird. Sobald ein gültiges Setup angezeigt wird, eröffnet die Strategie eine einzelne Position, schützt sie mit konfigurierbaren Stop-Loss- und Take-Profit-Abständen und kann optional beendet werden, wenn sich das Signal umkehrt oder ein Haltezeitlimit erreicht ist.

Die Standardkonfiguration repliziert den 15-minütigen Handelszeitrahmen mit einem 1-stündigen Bestätigungszeitrahmen und einer täglichen RSI-Länge von drei Perioden unter Verwendung des Kerzenöffnungspreises, genau wie beim ursprünglichen Experten.

## Handelslogik
1. **Fraktale Bias-Erkennung**
   - Fertige Kerzen aus dem Handelszeitrahmen und dem höheren Zeitrahmen werden über ein fraktales Fenster mit fünf Balken verarbeitet.
   - Die Parameter `Primary*Shift` und `Higher*Shift` legen fest, wie viele Balken die Strategie auf das neueste bestätigte Fraktal überprüft. Die Standardwerte stimmen mit dem ursprünglichen Wert von `3` überein, was bedeutet, dass der Code das Fraktal auswertet, das vor drei Kerzen bestätigt wurde.
   - Ein Abwärts-Fraktal (Swing-Tief) ohne begleitendes Aufwärts-Fraktal wird als bullisch (+1) behandelt. Ein Aufwärts-Fraktal ohne Abwärts-Fraktal wird als bärisch (-1) behandelt.
2. **Täglicher RSI-Filter**
   - Ein `RelativeStrengthIndex` mit dem konfigurierbaren `RsiPeriod` (Standard `3`) läuft im täglichen Zeitrahmen und verwendet den Kerzenöffnungspreis, passend zur MetaTrader-Implementierung.
   - Bei langen Setups muss der RSI unter `50 - RsiBuyOffset` liegen; Bei kurzen Setups muss der RSI über `50 + RsiSellOffset` liegen.
3. **Eintrittsbedingungen**
   - **Kaufen**: Beide Fraktal-Tracker melden +1 und der RSI-Filter ist bullisch. Die Strategie eröffnet eine Long-Position, wenn sie flach oder short ist, und sendet so genug Volumen, um auf die Long-Seite zu wechseln.
   - **Verkaufen**: Beide Fraktal-Tracker melden -1 und der RSI-Filter ist bärisch. Die Strategie eröffnet eine Short-Position, wenn sie flach oder lang ist, und sendet so genügend Volumen, um auf die Short-Seite zu wechseln.
4. **Positionsverwaltung**
   - Schützende Stop-Loss- und Take-Profit-Level werden unmittelbar nach der Positionsänderung auf der Grundlage von `StopLossPips` und `TakeProfitPips` multipliziert mit der Pip-Größe des Instruments berechnet.
   - Die Position kann geschlossen werden, wenn der Preis den Stopp oder das Ziel erreicht, wenn `ExpiryMinutes` abläuft oder wenn `CloseOnOppositeSignal` aktiviert ist und das Signal umkehrt.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `Volume` | Bestellvolumen, das für jeden Eintrag verwendet wird. | `0.1` |
| `TakeProfitPips` | Gewinnzielentfernung in Pips. Zum Deaktivieren auf `0` setzen. | `700` |
| `StopLossPips` | Stop-Loss-Distanz in Pips. Zum Deaktivieren auf `0` setzen. | `2500` |
| `ExpiryMinutes` | Maximale Haltezeit in Minuten, bevor ein Ausgang erzwungen wird. `0` deaktiviert den Timer. | `5555` |
| `CloseOnOppositeSignal` | Schließen Sie die aktive Position, wenn das Signal in die entgegengesetzte Richtung wechselt. | `false` |
| `PrimaryBuyShift` | Balken zurück, um das bullische Fraktal im Handelszeitrahmen zu untersuchen. | `3` |
| `HigherBuyShift` | Balken zurück, um das bullische Fraktal im höheren Zeitrahmen zu untersuchen. | `3` |
| `PrimarySellShift` | Balken zurück, um das rückläufige Fraktal im Handelszeitrahmen zu untersuchen. | `3` |
| `HigherSellShift` | Balken zurück, um das rückläufige Fraktal im höheren Zeitrahmen zu untersuchen. | `3` |
| `RsiBuyOffset` | Für lange Setups ist ein Versatz unter 50 erforderlich. | `3` |
| `RsiSellOffset` | Für kurze Setups ist ein Versatz über 50 erforderlich. | `3` |
| `RsiPeriod` | RSI Länge im täglichen Zeitrahmen. | `3` |
| `CandleType` | Kerzentyp des Handelszeitrahmens. | 15-Minuten-Kerzen |
| `HigherTimeFrame` | Kerzentyp für den Bestätigungszeitrahmen. | 1-Stunden-Kerzen |
| `DailyTimeFrame` | Kerzentyp, der für den täglichen RSI verwendet wird. | 1-Tages-Kerzen |

## Implementierungshinweise
- Die Strategie nutzt das High-Level-Kerzenabonnement API (`SubscribeCandles().Bind(...)`) und verwaltet Indikatoren intern, ohne sie über `Strategy.Indicators` offenzulegen, wie in den Richtlinien gefordert.
- Fractals werden über einen internen Helfer berechnet, der ein rollierendes Fünf-Kerzen-Fenster speichert und das Signal erst aktualisiert, nachdem ein Fraktal bestätigt wurde.
- RSI-Werte werden über `RelativeStrengthIndex.Process(...)` mit dem Kerzeneröffnungspreis abgerufen, entsprechend dem MetaTrader `PRICE_OPEN`-Modus.
- Es wird jeweils nur eine Position beibehalten. Marktaufträge drehen die Position bei Bedarf um, indem sie das zur Deckung eines bestehenden Risikos erforderliche Volumen hinzufügen.
- Die Pip-Größe wird aus `Security.PriceStep` und `Security.Decimals` geschätzt, wobei ein 10-facher Multiplikator für Vermögenswerte mit drei oder mehr Dezimalstellen verwendet wird, wodurch die Umrechnung von MetaTrader `Point` in Pip reproduziert wird.

## Nutzungstipps
- Die fraktalen Verschiebungen müssen groß genug sein, um sicherzustellen, dass der angeforderte Kerzenindex vorhanden ist. Bei einer Verschiebung von drei benötigt der Tracker mindestens fünf fertige Kerzen pro Zeitrahmen, bevor er Signale generiert.
- Wenn Sie Instrumente mit unterschiedlichen Tick-Größen handeln (z. B. Indizes oder Aktien), passen Sie `TakeProfitPips` und `StopLossPips` an die Pip-Definition des Instruments an.
- Das Deaktivieren von `CloseOnOppositeSignal` repliziert das ursprüngliche Verhalten des Expertenberaters (es war standardmäßig deaktiviert) und verlässt sich ausschließlich auf Stopps, Ziele oder den Ablauftimer für Exits.
- Die Strategie implementiert kein Martingal oder risikobasierte Größenbestimmung; Die MetaTrader-Lotberechnung basierte auf Kontomargenfunktionen, die in StockSharp nicht verfügbar sind. Verwenden Sie den Parameter `Volume` oder binden Sie die Strategie in einen Portfoliomanager ein, wenn eine dynamische Positionsgrößenbestimmung erforderlich ist.
