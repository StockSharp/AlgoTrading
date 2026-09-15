# Strategiediagramm „Clock Candle Breakout“
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm wählt jeden Tag nach der Uhrzeit eine Referenzkerze aus, merkt sich deren Hoch und Tief und handelt den Bruch dieser beiden Niveaus während der nächsten drei abgeschlossenen Kerzen. Ein EMA-20-Filter entscheidet, welche Seite des Ausbruchs handelbar ist, das Fenster schließt die Position bei seinem Ablauf, und eine Abkühlphase von zehn Kerzen hält das Diagramm nach jedem angenommenen Einstieg eine Weile aus dem Markt.

![schema](schema.svg)

## Strategieübersicht

- Dreißig-Minuten-Kerzen werden sowohl während ihrer Bildung als auch nach ihrem Abschluss geliefert. Ein Final value-Block trennt diesen Strom: Das Referenzniveau, der Fensterzähler und der Abkühlzähler sehen nur abgeschlossene Kerzen, während der Ausbruchstest den Schlusskurs der gerade entstehenden Kerze liest.
- Working time markiert die Referenzkerze: die abgeschlossene Kerze, deren Eröffnungszeit zwischen 02:30:00 und 02:59:59 liegt. Im Dreißig-Minuten-Zeitrahmen erfüllt genau eine Kerze pro Tag diese Bedingung, und die halbstündige Spanne lässt Raum, den Zeitrahmen zu ändern, ohne den täglichen Takt zu verlieren.
- Zwei Paare von Variable-Blöcken greifen das Niveau von dieser Kerze ab. In jedem Paar speichert die erste Variable das Hoch (bzw. das Tief) jeder abgeschlossenen Kerze und gibt es erst frei, wenn der Impuls von Working time eintrifft; die zweite hält den freigegebenen Wert und wiederholt ihn bei jeder Kerzenaktualisierung, sodass das Niveau zwischen zwei Referenzkerzen durchgehend auf der Leitung liegt.
- Derselbe Impuls startet einen auf drei gesetzten N values-Zähler. Er zählt abgeschlossene Kerzen und löst aus, sobald die dritte Kerze nach der Referenzkerze schließt – damit endet das Handelsfenster.
- Der Fensterzustand ist eine einzelne numerische Variable, die über eine Combination aus drei Quellen geschrieben wird: eins, wenn die Referenzkerze genommen wird, null, wenn der Drei-Kerzen-Zähler auslöst, und null, sobald ein Einstieg angenommen wurde. Ein Comparison gegen null macht aus dieser Zahl die Freigabe, die beide Einstiegszweige lesen, sodass ein Fenster höchstens eine Position ergibt.
- Eine Long-Position verlangt einen Schlusskurs über dem Referenzhoch und über dem EMA 20; eine Short-Position verlangt einen Schlusskurs unter dem Referenztief und unter dem EMA 20. Jede Seite ist ein Logical condition mit UND-Verknüpfung, das zusätzlich ein offenes Fenster, eine abgelaufene Abkühlphase und eine flache Position verlangt.
- Ein angenommener Einstieg schickt eine Market-Order über Modify position mit der Bedingung Open position, sodass ein Signal, das sich innerhalb derselben Kerze wiederholt, keine zweite Order auf die erste stapeln kann. Dasselbe Signal startet einen zweiten N values-Zähler über zehn abgeschlossene Kerzen; ein zweites Variable-Paar, verbunden durch eine eigene Combination, hält das Abkühl-Flag auf null, bis dieser Zähler abgelaufen ist, und setzt es danach wieder auf eins.
- Löst der Drei-Kerzen-Zähler aus, empfangen zwei Modify position-Blöcke mit der Bedingung Close position dieses Signal. Derjenige, dessen Seite der offenen Position entgegengesetzt ist, schließt sie zum Marktpreis; der andere hat nichts zu schließen und verwirft das Signal.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Während der drei abgeschlossenen Kerzen nach der Referenzkerze schickt ein Schlusskurs über dem Referenzhoch und über dem EMA 20 – bei abgelaufener Abkühlphase und flacher Position – einen Market-Kauf über Order Volume durch Open position.
- **Short-Einstieg**: Während derselben drei Kerzen schickt ein Schlusskurs unter dem Referenztief und unter dem EMA 20 – bei abgelaufener Abkühlphase und flacher Position – einen Market-Verkauf über Order Volume durch Open position.
- **Ausstieg**: Die Position wird über die Zeit geschlossen, nicht über den Preis: Löst der Drei-Kerzen-Fensterzähler aus, schließen die Close position-Blöcke zum Marktpreis alles, was offen ist. Es gibt keinen Stop-Loss, kein Kursziel und keine Trailing-Regel, sodass die Haltedauer das Fenster nie überschreitet; zudem schreibt der angenommene Einstieg eine Null in das Fenster, sodass dasselbe Fenster nicht zweimal gehandelt werden kann.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:30:00 | Zeitrahmen der Kerzenserie. Es werden sowohl entstehende als auch abgeschlossene Kerzen geliefert; nur die abgeschlossenen bestimmen das Referenzniveau und treiben die beiden Zähler an. |
| EMA Length | 20 | Periode des exponentiellen gleitenden Durchschnitts, der entscheidet, welche Seite des Ausbruchs gehandelt werden darf. Werte werden erst veröffentlicht, wenn der Durchschnitt ausgebildet ist, davor ist kein Einstieg möglich. |
| Reference From | 02:30:00 | Beginn der täglichen Spanne, in der die Referenzkerze gesucht wird, gelesen aus der Eröffnungszeit jeder abgeschlossenen Kerze. |
| Reference Until | 02:59:59 | Ende dieser Spanne. Zusammen mit dem Beginn muss sie genau eine Kerzeneröffnung pro Tag abdecken; das Standardpaar umfasst eine Dreißig-Minuten-Kerze. |
| Window Bars | 3 | Anzahl der abgeschlossenen Kerzen, die das Handelsfenster nach der Referenzkerze dauert. Derselbe Zähler schließt die Position bei dessen Ablauf. |
| Cooldown Bars | 10 | Anzahl der abgeschlossenen Kerzen, die nach einem angenommenen Einstieg gezählt werden, bevor das Diagramm wieder handeln darf. |
| Order Volume | 1 | Feste Stückzahl für beide Open position-Aktionen. Close position-Aktionen benötigen kein Volumen, da sie schließen, was offen ist. |

## Diagrammdetails

- Der Block [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) veröffentlicht entstehende wie abgeschlossene Kerzen gleichermaßen, und [Final value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/final_value.html) ist das Element, das sie trennt. Drei [Converters](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) lesen High und Low hinter Final value und Close davor – deshalb stammt das Niveau immer aus einer abgeschlossenen Kerze, während der Bruch gegen einen aktuellen Preis geprüft wird.
- [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) liest die Eröffnungszeit der abgeschlossenen Kerze, die hineingeschickt wird; deshalb ist sein Ausgang für eine Kerze pro Tag wahr und nicht für eine Spanne echter Uhrzeit. Die Reihenfolge im Diagramm ist wichtig: Die Konverter für High und Low sind davor verknüpft, sodass die speichernden [Variables](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) bereits die aktuelle Kerze halten, wenn der Impuls sie freigibt.
- Jeder der beiden [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)-Zähler wird durch ein Signal gestartet und zählt abgeschlossene Kerzen: drei für das Handelsfenster, zehn für die Abkühlphase. Ihre Ausgänge treffen in zwei [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html)-Blöcken auf die öffnenden und die sperrenden Werte, und jede Combination speist eine Zustands-Variable, die ihre Zahl bei jeder Kerzenaktualisierung erneut veröffentlicht.
- Der Block [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) liefert den EMA 20. Sieben [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Blöcke bilden die beiden Ausbruchstests, die beiden Trendtests, die Fensterfreigabe, die Abkühlfreigabe und die Prüfung auf flache Position gegen einen [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html)-Schnappschuss, der je Kerze gehalten wird; zwei [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)-Blöcke mit UND fassen sie zu den beiden Einstiegszweigen zusammen, und ein ODER-Block macht aus jedem der beiden Zweige das eine Signal, das die Abkühlphase startet.
- Vier [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Blöcke handeln: zwei mit Open position für die Einstiege und zwei mit Close position für den zeitgesteuerten Ausstieg. Jede Ausführung wird von einer Combination gesammelt und zusammen mit den Kerzen, dem EMA 20, beiden Referenzniveaus und allen vier Orderströmen im [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) gezeichnet.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
