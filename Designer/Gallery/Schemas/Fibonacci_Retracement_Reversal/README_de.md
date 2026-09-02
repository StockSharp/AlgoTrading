# Diagramm der Umkehrstrategie an Fibonacci-Retracements
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Spanne der letzten zwanzig Kerzen wird durch den Goldenen Schnitt geteilt, und die beiden entstehenden Retracement-Marken dienen als Umkehrzonen. Eine Kerze, die mit steigendem Körper auf der unteren Marke schließt, wird gekauft, eine mit fallendem Körper auf der oberen Marke verkauft; das Ende des Trades bestimmt die SimpleMovingAverage.

![schema](schema.svg)

## Strategieübersicht

- Highest und Lowest über dasselbe Fenster liefern Hoch und Tief der Spanne, ihre Differenz ist der Bereich, in dem die Marken gemessen werden.
- Die Kaufmarke liegt 0.618 der Spanne unter dem Hoch, die Verkaufsmarke 0.618 der Spanne über dem Tief; eine Kerze gilt als auf einer Marke, solange ihr Schluss weniger als zwei Prozent der Spanne davon entfernt ist.
- Beide Abstände werden als Anteil der Spanne gerechnet, deshalb arbeitet das Diagramm auf jedem Instrument und in jeder Preisgrößenordnung gleich.
- Für einen Einstieg braucht es zusätzlich einen bestätigenden Kerzenkörper und eine neutrale Position; alle Ausstiege übernimmt die SimpleMovingAverage, denn die Vorlage kennt weder Stopp noch Ziel.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt im Puffer um die untere Retracement-Marke, die Kerze ist steigend (Schluss über Eröffnung) und die Position ist neutral. Der Baustein kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Der Schlusskurs liegt im Puffer um die obere Retracement-Marke, die Kerze ist fallend (Schluss unter Eröffnung) und die Position ist neutral. Der Baustein verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird geschlossen, sobald eine Kerze unter der SimpleMovingAverage schließt, ein Short, sobald eine darüber schließt; beide Ausstiegsbausteine laufen im Schließmodus und lösen nur aus, wenn es etwas zu schließen gibt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Swing lookback | 20 | Anzahl der Kerzen, über die Hoch und Tief der Spanne genommen werden. |
| MA period | 20 | Periode der SimpleMovingAverage, an der die Ausstiege gemessen werden. |
| Fibonacci ratio | 0.618 | Retracement-Verhältnis, das beide Marken innerhalb der Spanne platziert. |
| Level buffer | 0.02 | Halbe Breite der Einstiegszone um eine Marke, als Anteil der Spanne. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Ein Kerzenbaustein versorgt Highest, Lowest und die SimpleMovingAverage sowie zwei Konverter, die Schluss- und Eröffnungskurs aus der Kerze holen.
- Zwei Formelbausteine machen aus den Kursen den durch die Spanne geteilten Abstand des Schlusskurses zu jeder Marke, sodass eine einzige Pufferkonstante für beide Seiten reicht.
- Jeder Einstieg läuft durch ein logisches UND aus drei Signalen: Marke, Kerzenkörper und die mit einer Nullkonstante verglichene Position.
- Die beiden Ausstiegsbausteine werden direkt von den Vergleichen mit dem gleitenden Durchschnitt ausgelöst und stehen im Schließmodus; alle vier Orderbausteine teilen sich eine Volumenkonstante.
- Bewusste Vereinfachungen: Das Original rechnet auf Minutenkerzen und pausiert nach jedem Trade 500 Balken, was kein Baustein abbilden kann; deshalb läuft das Diagramm auf Fünf-Minuten-Kerzen und handelt wieder, sobald die Bedingungen zurückkehren. Positionen halten einige Balken statt Tage; eine größere Periode des Durchschnitts verlängert sie.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
