# MA Break Impulse Buy-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert den Expertenberater „M.A break mt4 buy“ unter Verwendung des High-Level-API von StockSharp. Der Schwerpunkt liegt auf der Identifizierung starker bullischer Ausbrüche nach einer ruhigen Konsolidierung. Die Einstiegslogik sucht nach einer Folge von Filtern für den exponentiellen gleitenden Durchschnitt (EMA), einer ruhigen Marktphase und dann einer starken bullischen Impulskerze, die mit einem Ausbruch EMA interagiert. Die Strategie eröffnet nur **Long-Positionen**.

## Handelslogik
1. **EMA Trendfilter**
   - Zwei EMA-Paare werden für die zuvor abgeschlossene Kerze (`shift = 1`) ausgewertet.
   - `EMA(FirstFastPeriod)` muss größer als `EMA(FirstSlowPeriod)` sein.
   - `EMA(SecondFastPeriod)` muss größer als `EMA(SecondSlowPeriod)` sein.
2. **Impulskerzenauswahl**
   - Die Impulskerze ist der letzte abgeschlossene Balken (Schicht 1).
   - Der Eröffnungspreis muss über `TrendMaPeriod` EMA liegen.
   - Sein Tief muss den `BreakoutMaPeriod` EMA berühren oder unterschreiten.
   - Die Kerze muss bullisch sein (`Close > Open`).
   - Der Kerzenbereich muss zwischen `CandleMinSize` und `CandleMaxSize` liegen (umgerechnet aus Pips mit `Security.PriceStep`).
   - Der obere Docht darf `UpperWickLimit` Prozent des Kerzenbereichs nicht überschreiten. Der untere Docht muss mindestens `LowerWickFloor` Prozent des Bereichs ausmachen.
3. **Ruheriegel und Impulsstärke**
   - Die Strategie scannt `QuietBarsCount` Kerzen vor der Impulskerze (Verschiebungen ≥ 2) und zeichnet den maximalen Hoch-Tief-Bereich auf.
   - Dieser Ruhebereich muss größer als `QuietBarsMinRange` (Pips → Preis) sein.
   - Der Körper der Impulskerze (`Close - Open`) muss mindestens `ImpulseStrength × quietRange` sein.
4. **Positionsmanagement**
   - Eine Marktkauforder wird gesendet, wenn alle Bedingungen erfüllt sind und derzeit keine Position offen ist.
   - Schützende Stop-Loss- und Take-Profit-Orders werden über `StartProtection` verwaltet, wobei Pip-Eingaben verwendet werden, die über `Security.PriceStep` umgewandelt werden.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `FirstFastPeriod` | 20 | Schneller EMA wird im ersten Trendfilter verwendet. |
| `FirstSlowPeriod` | 30 | Langsamer EMA wird im ersten Trendfilter verwendet. |
| `SecondFastPeriod` | 30 | Schneller EMA für den zweiten Trendfilter. |
| `SecondSlowPeriod` | 50 | Langsamer EMA für den zweiten Trendfilter. |
| `TrendMaPeriod` | 30 | EMA, den die Impulskerzenöffnung überschreiten muss. |
| `BreakoutMaPeriod` | 20 | EMA, den das Tief der Impulskerze erreichen muss. |
| `QuietBarsCount` | 2 | Anzahl der ruhigen Kerzen, bevor der Impuls ausgewertet wird. |
| `QuietBarsMinRange` | 0,0 | Minimaler Ruhebereich (Pips). |
| `ImpulseStrength` | 1.1 | Auf den Ruhebereich angewendeter Multiplikator zur Validierung der Impulskörpergröße. |
| `UpperWickLimit` | 100,0 | Maximaler oberer Docht in Prozent des Kerzenbereichs. |
| `LowerWickFloor` | 0,0 | Minimaler unterer Docht in Prozent des Kerzenbereichs. |
| `CandleMinSize` | 0,0 | Minimal zulässiger Bereich der Impulskerze in Pips. |
| `CandleMaxSize` | 100,0 | Maximal zulässiger Bereich der Impulskerze in Pips. |
| `VolumeSize` | 0,01 | Handelsvolumen gesendet mit `BuyMarket`. Normalisiert, um `VolumeStep` auszutauschen. |
| `StopLossPips` | 20.0 | Stop-Loss-Distanz in Pips (umgerechnet mit `PriceStep`). |
| `TakeProfitPips` | 20.0 | Take-Profit-Distanz in Pips (umgerechnet mit `PriceStep`). |
| `CandleType` | 15-minütiger Zeitrahmen | Vom Connector angeforderter Kerzendatentyp. |

## Implementierungshinweise
- Die Strategie verwendet StockSharp hochrangige `Bind`-Abonnements, um die Indikatorberechnungen mit Kerzenaktualisierungen synchron zu halten.
- Alle Berechnungen basieren nur auf fertigen Kerzen (`CandleStates.Finished`).
- Quiet-Range- und Candle-Size-Filter konvertieren Pip-Werte intern mithilfe von `Security.PriceStep` in Preiseinheiten. Wenn das Instrument `PriceStep` nicht meldet, wird ein Fallback von `1` verwendet, der der MQL-Logik der Multiplikation mit dem Pip-Wert entspricht.
- `StartProtection` wird einmal während `OnStarted` aktiviert, sodass jede neue Position den konfigurierten Stop-Loss und Take-Profit erhält.
- Der Kerzenverlaufspuffer speichert nur die letzten `QuietBarsCount + 3` Einträge, um die Ruhephase und die Impulskerze effizient auszuwerten.

## Nutzungstipps
- Stellen Sie sicher, dass das angeschlossene Instrument `PriceStep`, `VolumeStep` und Volumengrenzwerte bereitstellt, damit Pip- und Volumenumrechnungen korrekt bleiben.
- Passen Sie EMA Perioden und Impulsparameter an die Volatilität des Instruments an. Ein niedrigerer `ImpulseStrength` reagiert auf kleinere Ausbrüche, während ein höherer Wert nur die stärksten Bewegungen herausfiltert.
- Die Strategie ist auf jeweils eine offene Position ausgelegt. Externe Positionen auf demselben Wertpapier können neue Einträge verhindern.
