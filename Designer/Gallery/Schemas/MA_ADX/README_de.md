# Diagramm der Strategie MA + ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Trenddiagramm mit Stärkefilter. Der ExponentialMovingAverage gibt die Marktseite vor, der Richtungsindex DX entscheidet, ob die Bewegung eine Position wert ist, und die Position wird aufgegeben, sobald der Schlusskurs auf die andere Seite des Durchschnitts zurückkehrt.

![schema](schema.svg)

## Strategieübersicht

- Der Schlusskurs wird mit einem ExponentialMovingAverage verglichen: oberhalb bedeutet long, unterhalb short.
- Der Baustein DirectionalIndex liefert den aus +DM und -DM berechneten DX-Wert; ein Einstieg ist nur erlaubt, solange DX über der Schwelle liegt.
- Eingestiegen wird ausschließlich aus der Neutralstellung, und jeder Ausstieg schließt genau die offene Position, sodass nie aufgestockt wird.
- Der Ausstieg beachtet die Trendstärke nicht: Sobald der Schlusskurs wieder auf der anderen Seite des Durchschnitts liegt, wird die Position unabhängig vom DX geschlossen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt über dem EMA, DX liegt über der Schwelle für die Trendstärke und die Position ist neutral. Die Order kauft das Grundvolumen und eröffnet einen Long.
- **Short-Einstieg**: Der Schlusskurs liegt unter dem EMA, DX liegt über der Schwelle für die Trendstärke und die Position ist neutral. Die Order verkauft das Grundvolumen und eröffnet einen Short.
- **Ausstieg**: Ein Long wird geschlossen, sobald eine Kerze unter dem EMA schließt, ein Short, sobald sie darüber schließt; die Schließbausteine beziehen ihr Volumen aus der offenen Position. Das Diagramm kennt weder Stop-Loss noch Take-Profit.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| EMA Length | 20 | Periode des exponentiellen Durchschnitts, der die Richtung vorgibt. |
| DX Length | 14 | Periode des Richtungsindex, der die Trendstärke misst. |
| Trend Strength | 25 | DX-Wert, oberhalb dessen eine neue Position erlaubt ist. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Indikatoren und einen Konverter, der den Schlusskurs herauszieht.
- Zwei Vergleichsbausteine bestimmen die Lage des Schlusskurses zum EMA und werden doppelt genutzt: Dasselbe Signal eröffnet die eine Seite und schließt die andere.
- Der Positionsbaustein speist drei Vergleiche mit null: die Neutralstellung sichert die Einstiege, long und short sichern die beiden Ausstiege.
- Die Einstiegsbausteine arbeiten mit der Bedingung zum Eröffnen und beziehen ihr Volumen aus einer gemeinsamen Konstante, die Ausstiegsbausteine mit der Bedingung zum Schließen und berechnen das Volumen selbst.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
