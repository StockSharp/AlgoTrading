# Diagramm der Mean-Reversion-Strategie bei niedriger Volatilität
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Mean Reversion funktioniert, wenn der Markt auf der Stelle tritt, und schadet im Trend. Deshalb handelt dieses Diagramm nur, solange der Markt ruhig ist. Ruhe wird ohne jede absolute Zahl definiert: Die aktuelle Average True Range wird mit ihrem eigenen geglätteten Durchschnitt verglichen, und erst wenn sie unter einem Anteil dieses Durchschnitts liegt, wird eine Position eröffnet.

![schema](schema.svg)

## Strategieübersicht

- Volatilität wird an sich selbst gemessen: Eine AverageTrueRange speist eine SmoothedMovingAverage, und das Verhältnis der beiden ist der gesamte Regimefilter, sodass sich das Diagramm ohne Neukalibrierung auf jedes Instrument übertragen lässt.
- Die Glättung bildet den rekursiven Durchschnitt des Originalcodes exakt nach, denn SmoothedMovingAverage rechnet mit derselben Formel: Durchschnitt mal Länge minus eins, plus neuer Wert, geteilt durch die Länge.
- Der faire Wert ist eine gewöhnliche SimpleMovingAverage: Ein Schlusskurs darunter wird gekauft, einer darüber verkauft, aber nur im ruhigen Regime und nur aus der Neutralstellung.
- Das Original arbeitet auf Minutenkerzen und sperrt die gesamte Strategie nach jedem Trade für 500 Bars, samt ihrer Ausstiege. Die mitgelieferte Historie besteht aus Fünf-Minuten-Daten, daher läuft das Diagramm auf Fünf-Minuten-Kerzen; die Sperre ist nicht nachgebildet, weil der Designer keinen zustandsbehafteten Bar-Zähler kennt, und das Diagramm handelt deshalb häufiger als das Original.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Average True Range liegt unter dem Ruheniveau, der Schlusskurs steht unter dem gleitenden Durchschnitt und die Position ist neutral. Die Order kauft das eingestellte Volumen.
- **Short-Einstieg**: Die Average True Range liegt unter dem Ruheniveau, der Schlusskurs steht über dem gleitenden Durchschnitt und die Position ist neutral. Die Order verkauft das eingestellte Volumen.
- **Ausstieg**: Ein Long wird geschlossen, sobald der Schlusskurs wieder über den gleitenden Durchschnitt zurückkehrt, ein Short, sobald er wieder darunter fällt. Die Ausstiege ignorieren den Volatilitätsfilter bewusst, sodass ein Trade auch dann zurückgegeben wird, wenn der Markt bereits aufgewacht ist. Stop-Loss und Take-Profit gibt es nicht, wie in der Originalstrategie.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode des gleitenden Durchschnitts, der als fairer Wert dient. |
| ATR Length | 14 | Glättungsperiode der Average True Range, also der aktuellen Volatilität. |
| ATR averaging length | 20 | Länge, über die die Average True Range zu ihrem eigenen Durchschnitt geglättet wird. |
| Quiet threshold, % | 80 | Anteil der durchschnittlichen Volatilität in Prozent, unterhalb dessen der Markt als ruhig gilt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Konverter für den Schlusskurs, den gleitenden Durchschnitt und die Average True Range; die Range geht anschließend in einen zweiten Indikatorbaustein, der sie glättet.
- Ein Formelbaustein macht aus der geglätteten Range und dem herausgeführten Prozentsatz das Ruheniveau, ein Vergleichsbaustein stellt die rohe Range dagegen.
- Zwei Vergleichsbausteine entscheiden, auf welcher Seite des Durchschnitts der Schlusskurs liegt, und werden doppelt genutzt: Dieselbe Bedingung, die einen Long eröffnet, schließt einen Short.
- Die beiden Einstiegs-UNDs verbinden je drei Bedingungen — Kurs, Volatilität und neutrale Position —, die beiden Ausstiegs-UNDs nur Kurs und Position, weshalb die Ausstiege in jedem Regime greifen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
