# Diagramm der Outside-Bar-Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Outside Bar ist eine Kerze, die die gesamte Spanne der vorherigen verschluckt: ein höheres Hoch und ein tieferes Tief in derselben Bar. Beide Seiten bekamen innerhalb einer einzigen Kerze ihre Gelegenheit, und eine hat gewonnen. Das Diagramm liest den Gewinner deshalb am Körper der Bar selbst ab: Ein bullischer Outside Bar wird gekauft, ein bärischer verkauft. Danach entscheidet ein einfacher gleitender Durchschnitt der Schlusskurse, wann der Trade losgelassen wird.

![schema](schema.svg)

## Strategieübersicht

- Der Outside Bar ist aus einfachen Bausteinen gebaut: Konverter lesen Hoch, Tief, Eröffnung und Schluss der abgeschlossenen Kerze, zwei Bausteine für den Vorwert halten Hoch und Tief der vorangegangenen Kerze.
- Zwei Vergleiche bilden die Figur — Hoch über dem vorherigen Hoch und Tief unter dem vorherigen Tief — und beide müssen gleichzeitig gelten.
- Die Richtung kommt aus dem Körper der Kerze selbst, nicht aus einem Trendfilter: Schluss über der Eröffnung heißt kaufen, darunter verkaufen.
- Der einfache gleitende Durchschnitt ist am Einstieg nicht beteiligt und dient nur als Ausstiegslinie, genau wie im Original.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Kerze hat beide Extreme der vorherigen genommen, sie schloss über ihrer eigenen Eröffnung und es besteht keine Position. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Die Kerze hat beide Extreme der vorherigen genommen, sie schloss unter ihrer eigenen Eröffnung und es besteht keine Position. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird geschlossen, sobald eine Kerze unter dem gleitenden Durchschnitt schließt, ein Short, sobald eine darüber schließt, beides über Bausteine zur Positionsänderung im Schließmodus, genau wie im Original. Stop-Loss und Take-Profit gibt es nicht, weil der Originalcode beides nicht kennt. Weggelassen ist die Pause von mehreren hundert Kerzen, die das Original nach jedem Ein- und Ausstieg einhält: Ein Balkenzähler lässt sich nur bauen, indem ein Signal ins Diagramm zurückgeführt wird, was den Graphen zu einer Schleife schließen würde. Deshalb wird hier jeder Outside Bar gehandelt und entsprechend deutlich häufiger.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts, der die Trades schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. Die Originalstrategie rechnet auf Minutenkerzen; hier sind es fünf Minuten, passend zur mitgelieferten Historie. |

## Diagrammdetails

- Der Kerzenbaustein speist fünf Zweige: vier Konverter für Eröffnung, Hoch, Tief und Schluss sowie den gleitenden Durchschnitt.
- Hoch und Tief laufen jeweils zugleich zwei Wege — direkt in einen Vergleich und in einen Baustein für den Vorwert —, sodass der Vergleich das Extrem dieser Kerze gegen das der vorherigen stellt.
- Jedes logische UND sammelt vier Flags: das höhere Hoch, das tiefere Tief, die Richtung des Körpers und die Positionsprüfung aus Positionsbaustein und Nullkonstante.
- Beide Einstiegsbausteine senden Marktorders und beziehen ihr Volumen aus einer gemeinsamen Konstante; die beiden Ausstiegsbausteine werden direkt von den Vergleichen mit dem Durchschnitt ausgelöst und greifen nur, wenn es etwas zu schließen gibt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
