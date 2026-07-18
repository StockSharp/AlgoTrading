# MACD Sample Trendfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein direkter Port des klassischen MetaTrader 5 **MACD Sample**-Expertenberaters. Sie verwendet MACD-Kreuzungen, gefiltert durch einen EMA-Trendindikator. Aufträge werden mit der Standard-Eigenschaft `Volume` dimensioniert, während das Risikomanagement auf konfigurierbaren Pip-Schwellenwerten für das MACD-Histogramm, Take Profit und Trailing Stop basiert.

## Kernlogik

- **Indikatoren**
  - `MovingAverageConvergenceDivergenceSignal` mit Perioden *(12, 26, 9)* liefert MACD- und Signallinien.
  - `ExponentialMovingAverage` mit Periode *26* fungiert als Trendfilter.
- **Einstiegskriterien**
  - **Long**: MACD liegt unter null, kreuzt die Signallinie nach oben, hat eine Magnitude über dem *MACD Open Level* und die EMA steigt.
  - **Short**: MACD liegt über null, kreuzt die Signallinie nach unten, hat eine Magnitude über dem *MACD Open Level* und die EMA fällt.
- **Ausstiegskriterien**
  - MACD kreuzt gegen die Position mit einer Magnitude über dem *MACD Close Level*.
  - Take Profit erreicht die konfigurierte Pip-Distanz vom Einstiegspreis.
  - Trailing Stop (falls durch Gewinn > Trailing-Distanz aktiviert) wird ausgelöst.
- **Trailing-Stop-Mechanik**
  - Long-Positionen aktivieren den Trailing Stop, sobald der Hochkurs den Einstiegspreis um die Trailing-Distanz überschreitet. Der Stop wird dann bei *Hoch − Trailing-Distanz* gehalten.
  - Short-Positionen aktivieren den Trailing Stop, sobald der Tiefstkurs unter den Einstiegspreis um die Trailing-Distanz fällt. Der Stop wird bei *Tief + Trailing-Distanz* gehalten.

## Parameter

| Parameter | Standardwert | Beschreibung |
|-----------|--------------|--------------|
| `FastPeriod` | 12 | Schnelle EMA-Periode innerhalb des MACD. |
| `SlowPeriod` | 26 | Langsame EMA-Periode innerhalb des MACD. |
| `SignalPeriod` | 9 | Signal-EMA-Periode innerhalb des MACD. |
| `TrendPeriod` | 26 | Länge des EMA-Trendfilters. |
| `MacdOpenLevelPips` | 3 | Minimale MACD-Magnitude (in Pips), die zum Öffnen eines Trades erforderlich ist. |
| `MacdCloseLevelPips` | 2 | Minimale MACD-Magnitude (in Pips), die zum Schließen eines Trades bei Kreuzung erforderlich ist. |
| `TakeProfitPips` | 50 | Take-Profit-Distanz in Pips. |
| `TrailingStopPips` | 30 | Trailing-Stop-Distanz in Pips. Auf 0 setzen, um Trailing zu deaktivieren. |
| `CandleType` | 15-Minuten-Zeitrahmen | Kerzentyp für Berechnungen. |

### Pip-Konvertierung

Der ursprüngliche Expertenberater verwendete MetaTraders Pip-Normalisierung (Multiplikation mit 10 für 3/5-stellige Symbole). Die Konvertierung folgt derselben Idee durch Inspektion von `Security.PriceStep`:

- Wenn der Preisschritt 3 oder 5 Nachkommastellen hat, ist die Pip-Größe `PriceStep * 10`.
- Andernfalls entspricht die Pip-Größe `PriceStep`.
- Wenn der Preisschritt nicht verfügbar ist, fallen Pip-basierte Schwellenwerte auf Rohwerte zurück.

## Verhaltenshinweise

- Positionen werden geschlossen, bevor neue Signale ausgewertet werden, was die MT5-Implementierung spiegelt.
- `LogInfo`-Anweisungen melden Einstiege, Ausstiege und Trailing-Stop-Aktualisierungen zur einfacheren Fehlerbehebung.
- Schutzorders werden nicht automatisch platziert; Ausstiege werden innerhalb von `ProcessCandle` verwaltet, um die EA-Logik zu imitieren.
- Verwenden Sie `Volume`, um die Basis-Trade-Größe zu definieren. Umkehrungen gleichen automatisch die aktuelle Exposition aus, indem `Math.Abs(Position)` zum Auftragsvolumen hinzugefügt wird.

## Unterschiede zur MQL5-Version

- Die Verarbeitung erfolgt auf abgeschlossenen Kerzen statt bei jedem Tick. Dies vermeidet wiederholte Signale und hält das Verhalten deterministisch.
- Trailing-Stop- und Take-Profit-Prüfungen verwenden Kerzenhochs und -tiefs, um Bid/Ask-Treffer aus dem ursprünglichen EA anzunähern.
- Wenn `Security.PriceStep` fehlt, wirken Pip-Parameter als absolute Preisabstände und müssen manuell eingestellt werden.

Passen Sie die Pip-Schwellenwerte und den Kerzentyp an das gehandelte Instrument an, insbesondere beim Portieren auf Märkte mit unterschiedlichen Tick-Größen.
