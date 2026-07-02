# Einfache Kloss-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Kloss Simple Strategy** ist eine direkte Umsetzung des MetaTrader 4 Expertenberaters `Kloss_.mq4`. Es rekonstruiert die ursprüngliche Handelsidee unter Verwendung des High-Level-API von StockSharp und hält den Indikatorsatz identisch: einen exponentiellen gleitenden Durchschnitt (EMA), der auf gewichteten Schlusskursen berechnet wird, den Commodity Channel Index (CCI) und den Stochastic-Oszillator. Signale werden aus der zuvor abgeschlossenen Kerze generiert und spiegeln die Ein-Balken-Verschiebungslogik in der MQL-Version wider. Die Positionsgröße kann entweder auf einem festen Auftragsvolumen oder auf einem Risikoprozentsatz des Portfoliowerts basieren, genau wie die ursprünglichen Lot-Berechnungsregeln.

## Kernidee

1. Überwachen Sie den Momentumkontext mit den Schwellenwerten **CCI** und **Stochastic** um ihre neutralen Niveaus.
2. Bestätigen Sie Momentumsignale mit einem kurzfristigen **EMA** des gewichteten Schlusskurses.
3. Geben Sie Positionen nur dann ein, wenn die vorherige Kerze alle Signalbedingungen erfüllt, um vorzeitige Geschäfte aufgrund unvollständiger Marktdaten zu verhindern.
4. Erlauben Sie mehrere Einträge in die gleiche Richtung bis zu einem konfigurierbaren Limit und emulieren Sie den Parameter „MaxOrders“ aus dem MT4-Skript.

## Anzeigekonfiguration

- **EMA (MaPeriod)**: Verwendet den gewichteten Abschluss `(Close * 2 + High + Low) / 4`, um `PRICE_WEIGHTED` von MetaTrader abzugleichen. Fungiert als kurzfristiger Trendfilter.
- **CCI (CciPeriod)**: Bewertet Momentumabweichungen vom Durchschnittspreis. Der Schwellenwert `±CciLevel` definiert aggressive gegenüber konservativen Einträgen.
- **Stochastic (StochasticKPeriod / DPeriod / Smooth)**: Verwendet die Hauptlinie %K, um überkaufte oder überverkaufte Bedingungen relativ zum neutralen 50-Niveau zu erkennen. Die Abweichung von 50 wird durch `StochasticLevel` gesteuert.

Alle Indikatoren arbeiten mit der durch `CandleType` definierten primären Kerzenserie. Die Strategie aktualisiert die Indikatorwerte nur für fertige Kerzen und gewährleistet so ein stabiles Backtesting und Live-Verhalten.

## Handelslogik

### Lange Einrichtung

1. Der vorherige Kerzenschluss liegt über dem vorherigen EMA-Wert.
2. Der vorherige CCI-Wert liegt unter `-CciLevel`, was auf eine überverkaufte Dynamik hinweist.
3. Der vorherige %K-Wert von Stochastic liegt unter `50 - StochasticLevel`, was eine überverkaufte Schwankung bestätigt.
4. Wenn die Bedingungen erfüllt sind, wird jedes Short-Engagement geschlossen und eine neue Long-Position eröffnet, sofern die Anzahl der bestehenden Long-Orders unter `MaxOrders` liegt.

### Kurze Einrichtung

1. Der vorherige Kerzenschluss liegt unter dem vorherigen EMA-Wert.
2. Der vorherige CCI-Wert liegt über `+CciLevel`, was auf eine überkaufte Dynamik hinweist.
3. Der vorherige Stochastic %K-Wert liegt über `50 + StochasticLevel`, was eine überkaufte Schwankung bestätigt.
4. Wenn die Bedingungen erfüllt sind, wird jede Long-Position geschlossen und eine neue Short-Position eröffnet, vorbehaltlich des `MaxOrders`-Limits.

### Exit-Management

- **Stop Loss / Take Profit**: Optionale absolute Abstände in Instrumentenpunkten. Wenn einer der Werte größer als Null ist, wird der integrierte Positionsschutz von StockSharp aktiviert.
- **Gegensignal**: Vor dem Öffnen in die entgegengesetzte Richtung wird die aktuelle Position geschlossen, um den ursprünglichen Expert Advisor nachzuahmen.

## Positionsgrößen

- **OrderVolume**: Feste Standardgröße, die den Parameter `Lots` von MT4 repliziert.
- **Risikoprozentsatz**: Wenn der Wert größer als Null ist, berechnet die Strategie die Handelsgröße als Prozentsatz des Portfoliowerts. Sofern verfügbar, werden die Margin-Anforderungen von Instrumenten verwendet, andernfalls wird auf die preisbasierte Größenbestimmung zurückgegriffen, wodurch das `Lots == 0`-Verhalten des MQL-Codes reproduziert wird.
- **MaxOrders**: Begrenzt das kumulative Volumen pro Richtung, indem eine Belichtung von bis zu `MaxOrders * OrderVolume` zugelassen wird.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Basisauftragsgröße, die verwendet wird, wenn `RiskPercentage` Null ist. |
| `MaPeriod` | Länge des EMA basierend auf gewichteten Schlusskursen. |
| `CciPeriod` | Anzahl der Balken, die in der CCI-Berechnung verwendet werden. |
| `CciLevel` | Absoluter CCI-Schwellenwert für die Signalgenerierung. |
| `StochasticKPeriod` | Lookback für die Zeile Stochastic %K. |
| `StochasticDPeriod` | Gleitender Durchschnittszeitraum für die %D-Linie. |
| `StochasticSmooth` | Zusätzliche Glättung auf %K angewendet. |
| `StochasticLevel` | Abweichung von 50 wird zur Erkennung von Überkauft/Überverkauft verwendet. |
| `MaxOrders` | Maximal zulässige Anzahl an Einträgen pro Richtung. |
| `StopLossPoints` | Optionale Stop-Loss-Distanz in Preispunkten. |
| `TakeProfitPoints` | Optionale Take-Profit-Distanz in Preispunkten. |
| `RiskPercentage` | Portfolio-Prozentsatz für dynamische Positionsgrößenbestimmung. |
| `CandleType` | Für alle Berechnungen verwendete Kerzenreihe. |

## Praktische Hinweise

- Funktioniert am besten bei Intraday-Daten, bei denen kurzfristige Oszillatoren schnell auf Preisschwankungen reagieren.
- Der gewichtete Schlusskurs sorgt dafür, dass der EMA reaktionsfähig bleibt und gleichzeitig den Hoch-/Tief-Bereich der Kerze berücksichtigt.
- Da jede Entscheidung auf der vorherigen Kerze basiert, vermeidet die Strategie ein Neuzeichnen innerhalb eines Balkens und bleibt bei historischen Tests deterministisch.
- Das Risikomanagement sollte an den Vertragsspezifikationen des Brokers ausgerichtet sein, sodass `OrderVolume` und `MaxOrders` den ausführbaren Handelsgrößen entsprechen.
