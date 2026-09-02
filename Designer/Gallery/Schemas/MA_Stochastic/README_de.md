# Diagramm der Strategie aus gleitendem Durchschnitt und Stochastik
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei Bausteine entscheiden gemeinsam: Die SimpleMovingAverage bestimmt, auf welcher Marktseite das Diagramm überhaupt handeln darf, und die StochasticK wartet auf eine Bewegung gegen diese Seite, bevor die Order herausgeht. Die Position wird zurückgegeben, sobald der Kurs auf der anderen Seite derselben Linie schließt.

![schema](schema.svg)

## Strategieübersicht

- Die Richtung ergibt sich aus dem Schlusskurs gegenüber der SimpleMovingAverage: oberhalb kommen nur Longs infrage, unterhalb nur Shorts.
- Der Einstieg selbst ist antizyklisch - die %K-Linie muss für einen Long in der überverkauften und für einen Short in der überkauften Zone stehen; das Diagramm kauft also Rücksetzer im Aufwärtstrend und verkauft Erholungen im Abwärtstrend.
- StochasticK ist genau jenes %K, das die Originalstrategie von Hand berechnet hat: 100 * (Close - tiefstes Low) / (höchstes High - tiefstes Low) über die letzten N Kerzen.
- Derselbe gleitende Durchschnitt ist auch die Ausstiegslinie; Stop-Loss oder Take-Profit gibt es im Diagramm nicht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt über der SimpleMovingAverage, StochasticK unter der überverkauften Marke und es besteht keine Position. Die Order kauft ein Lot zum Marktpreis.
- **Short-Einstieg**: Der Schlusskurs liegt unter der SimpleMovingAverage, StochasticK über der überkauften Marke und es besteht keine Position. Die Order verkauft ein Lot zum Marktpreis.
- **Ausstieg**: Ein Long wird auf der ersten Kerze geschlossen, die unter dem Durchschnitt schließt, ein Short auf der ersten Kerze darüber; beide Schließbausteine beziehen ihr Volumen aus der offenen Position.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode der SimpleMovingAverage, die den Trend filtert und die Position schließt. |
| %K Length | 14 | Anzahl der Kerzen, über die die %K-Linie zurückblickt. |
| %K Oversold | 20 | Marke, unterhalb derer %K als überverkauft gilt und ein Long erlaubt ist. |
| %K Overbought | 80 | Marke, oberhalb derer %K als überkauft gilt und ein Short erlaubt ist. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist drei Zweige: den Konverter mit dem Schlusskurs, die SimpleMovingAverage und den Indikator StochasticK.
- Zwei Vergleiche stellen den Schlusskurs dem Durchschnitt gegenüber, zwei weitere %K den Schwellenkonstanten, und einer vergleicht die Position mit null.
- Jedes logische UND verbindet eine Trendbedingung, eine Stochastik-Bedingung und die Nullpositionsprüfung und löst dann einen Baustein aus, der nur aus der Neutralstellung eröffnet.
- Die Trendvergleiche werden vom Ausstieg mitbenutzt: Dasselbe Signal, das einen Short erlaubt, schließt einen Long - so bleibt das Diagramm klein. Der Barzähler, der die Originalstrategie nach jedem Trade 100 Kerzen lang pausieren ließ, hat keinen eigenen Baustein und entfällt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
