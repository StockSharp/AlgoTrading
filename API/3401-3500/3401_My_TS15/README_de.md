# Strategie My TS15 Moving Average Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

This strategy reproduces the behaviour of the original **my_ts15.mq5** expert advisor by managing trailing stop orders around an existing net position. Ein linearer gewichteter gleitender Durchschnitt (LWMA) steuert die Stop-Platzierung und kann durch andere Glättungsmethoden ersetzt werden. The logic continuously:

* Reads the moving average value from a configurable number of completed candles.
* Vergleicht den Preisfortschritt mit der gleitenden Durchschnittsspur und preisbasierten Offsets.
* Moves the protective stop order only when the new level improves the previous one by at least the specified step.
* Erzwingt optional eine maximale Verlustdistanz durch Festlegen des Stopps oder sofortige Liquidierung der Position, wenn das Limit überschritten wird.

The strategy does not produce entry signals. Es soll zusammen mit anderen Komponenten (manuell oder automatisiert) laufen, die Positionen für dasselbe Wertpapier eröffnen.

## Handelslogik

1. Abonnieren Sie die ausgewählte Kerzenserie und binden Sie einen Indikator für den gleitenden Durchschnitt mithilfe des StockSharp-High-Level-API.
2. Sobald eine Kerze fertig ist, speichern Sie das Indikatorergebnis und erhalten den Wert, der `MaBarsTrail + MaShift` Balken hinter dem aktuellen Balken liegt.
3. Convert the point-based settings to absolute price distances using the instrument tick size.
4. For long positions, choose the lowest of:
   * The moving average minus its offset.
   * Der aktuelle Preis abzüglich des „im Gewinn“-Ausgleichs.
Anschließend spannen Sie die Spur auf die „In-Loss“-Distanz und optional auf den maximal erlaubten Loss.
5. For short positions, choose the highest of:
   * The moving average plus its offset.
   * Der aktuelle Preis zuzüglich des „im Gewinn“-Offsets.
Anschließend spannen Sie die Spur auf die „In-Loss“-Distanz und optional auf den maximal erlaubten Loss.
6. Aktualisieren Sie die Stoppreihenfolge nur, wenn die Verbesserung `TrailStepPoints` überschreitet (es sei denn, sie beträgt Null; in diesem Fall wird jede Verbesserung akzeptiert).
7. If the price breaches the maximum loss distance and `EnforceMaxStopLoss` is enabled, the strategy closes the position immediately.

Alle Preiseingaben verwenden den in `MaPrice` angegebenen Kerzenpreis, der mit der ursprünglichen MQL-Einstellung übereinstimmt, bei der der Indikator mit der `PRICE_WEIGHTED`-Reihe gespeist wird.

## Parameter

| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `MaPeriod` | `50` | Length of the moving average used as the trailing backbone. |
| `MaShift` | `0` | Zusätzliche Verschiebung (in Balken), die beim Abtasten des gleitenden Durchschnittswerts angewendet wird. |
| `MaMethod` | `LinearWeighted` | Glättungsmethode des gleitenden Durchschnitts (einfach, exponentiell, geglättet, linear gewichtet). |
| `MaPrice` | `Weighted` | Candle price fed to the moving average. |
| `MaBarsTrail` | `1` | Anzahl der abgeschlossenen Balken zwischen der aktuellen Kerze und der Stichprobe des gleitenden Durchschnitts. |
| `TrailBehindMaPoints` | `5` | Distance in points kept between the stop and the moving average. |
| `TrailBehindPricePoints` | `30` | Distance in points kept behind the price when the position is profitable. |
| `TrailBehindNegativePoints` | `60` | Distanz in Punkten, die hinter dem Preis gehalten wird, wenn die Position verliert. |
| `TrailStepPoints` | `0` | Minimum improvement (in points) required before moving the stop. Zero replicates the “always update” behaviour. |
| `EnforceMaxStopLoss` | `false` | Wenn aktiviert, wird der Stop auf den maximal zulässigen Verlust begrenzt und die Position liquidiert, wenn der Preis dieses Limit überschreitet. |
| `MaxStopLossPoints` | `100` | Maximum allowed loss distance in points. |
| `ShowIndicator` | `true` | Zeichnen Sie den gleitenden Durchschnitt und die Handelsmarkierungen auf dem Diagramm ein, wenn die Benutzeroberfläche verfügbar ist. |
| `CandleType` | `M1` | Candle data type driving the calculations. |

Alle punktbasierten Eingaben werden über die aus `Security.PriceStep` berechnete Pip-Größe des Instruments in Preisabstände umgewandelt.

## Konvertierungshinweise

* Der MQL-Experte hat das MA-Handle manuell aktualisiert. Die StockSharp-Implementierung verwendet `BindEx`, um den Indikator zu verarbeiten, ohne auf interne Puffer zuzugreifen oder `GetValue` aufzurufen.
* Bid/Ask-Preise sind für fertige Kerzen nicht direkt verfügbar, daher verwenden die nachfolgenden Berechnungen den von `MaPrice` ausgewählten Kerzenpreis. Dadurch bleibt das Verhalten konsistent, da das ursprüngliche Skript den Indikator mit demselben gewichteten Preis versorgt und ihn mit Bid/Ask-Ticks verglichen hat.
* `PositionModify` is replaced by cancelling and recreating protective stop orders (`SellStop` for long, `BuyStop` for short). The strategy stores the last stop level to mimic the MetaTrader trailing thresholds.
* Das optionale erzwungene Schließen (`pre_init`) folgt der ursprünglichen Logik: Sobald der Markt über `MaxStopLossPoints` hinausgeht, wird die Position sofort geschlossen.
* No entry logic has been added; Benutzer sollten dieses nachlaufende Modul mit ihrem eigenen Signalanbieter kombinieren.

## Nutzungstipps

1. Hängen Sie die Strategie an dasselbe Wertpapier an, das die Positionen eröffnet.
2. Passen Sie die Punktabstände an die Tick-Größe des Instruments an (Forex-Symbole verwenden im Allgemeinen „Pip“-Werte, CFDs erfordern möglicherweise andere Multiplikatoren).
3. Setzen Sie `TrailStepPoints` auf einen positiven Wert, um die Auftragsabwanderung bei illiquiden Instrumenten zu reduzieren.
4. Deaktivieren Sie `EnforceMaxStopLoss`, wenn bereits ein anderer Risikomanager die Abstände für harte Stopps kontrolliert.
5. Keep `ShowIndicator` enabled while tuning the parameters to visualise the moving average and trailing behaviour.
