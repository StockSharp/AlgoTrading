# Diagramm der Strategie Bullish Engulfing mit SMA-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Engulfing-Kerze sagt, dass die Seite, die die vorherige Bar beherrschte, gerade überrannt wurde. Für sich genommen kommt das viel zu häufig vor, deshalb entscheidet ein einfacher gleitender Durchschnitt, wo das Signal genommen wird: ein bullisches Engulfing wird nur unterhalb des Durchschnitts gekauft, ein bärisches nur oberhalb verkauft. Derselbe Durchschnitt ist auch das Ziel, an dem der Trade geschlossen wird.

![schema](schema.svg)

## Strategieübersicht

- Zwei Bausteine des Kerzenmuster-Indikators tragen die fertigen Muster Bullish Engulfing und Bearish Engulfing, sodass die Form ohne eigene Formel erkannt wird.
- Ein einfacher gleitender Durchschnitt der Schlusskurse teilt den Chart in eine billige und eine teure Hälfte.
- Das Muster wird nur in der billigen Hälfte gekauft und nur in der teuren verkauft; damit wird das Diagramm zu einem Mean-Reversion-Beispiel und nicht zu einem Ausbruchsbeispiel.
- Die Positionsprüfung sorgt dafür, dass ein Muster nur bei neutraler Position gehandelt wird.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Musterbaustein meldet ein bullisches Engulfing, die Kerze schloss unter dem gleitenden Durchschnitt und es besteht keine Position. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Der Musterbaustein meldet ein bärisches Engulfing, die Kerze schloss über dem gleitenden Durchschnitt und es besteht keine Position. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird geschlossen, sobald eine Kerze über dem gleitenden Durchschnitt schließt, ein Short, sobald eine unter ihm schließt, beides über Bausteine zur Positionsänderung im Schließmodus. Die Originalstrategie steigt auf derselben Seite des Durchschnitts aus, auf der sie eingestiegen ist, und hält den Trade dazwischen über eine Pause von mehreren hundert Bars; einen Bar-Zähler gibt es hier als Baustein nicht, deshalb ist die Rückkehr zum Durchschnitt der Ausstieg — die nächstliegende Regel, die weiterhin sinnvoll handelt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Periode des einfachen gleitenden Durchschnitts, der die Muster filtert und die Trades schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist vier Zweige: die beiden Musterindikatoren, den gleitenden Durchschnitt und einen Konverter für den Schlusskurs.
- Zwei Vergleichsbausteine stellen den Schlusskurs auf die eine oder andere Seite des Durchschnitts; dieselben zwei Signale dienen als Einstiegsfilter und als Ausstiegsauslöser.
- Der Positionsbaustein wird mit einer Nullkonstante verglichen, und das Ergebnis sichert beide Einstiege ab.
- Jedes logische UND verbindet ein Muster, einen Filter und die Positionsprüfung und löst einen Baustein zur Positionsänderung aus; beide Einstiegsorders beziehen ihr Volumen aus einer gemeinsamen Konstante, die beiden Schließbausteine brauchen keines.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
