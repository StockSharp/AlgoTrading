# Recovery Target Breakout – Strategiediagramm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bei den Einstiegen geht es hier nur um eines: um eine Kerze, die deutlich breiter ist als alles, was der Markt zuletzt gezeigt hat. Beim Ausstieg geht es um Geld statt um den Kurs — das offene Ergebnis der Position wird gegen ein Ziel gemessen, das das Diagramm in einer Variablen hält, und dieses Ziel ist keine Konstante. Ein Ausstieg mit Verlust vervielfacht es, ein Ausstieg mit Gewinn setzt es auf den Basiswert zurück, sodass jeder Trade weiß, was der vorherige gekostet hat.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünfzehn-Minuten-Kerzen speisen vier Konverter, die aus jedem Balken Hoch, Tief, Eröffnung und Schluss herausziehen.
- Eine Formel zieht das Tief vom Hoch ab und erhält so die Spanne des Balkens, während ein Average true range-Indikator misst, wie groß die Spanne über die letzten zehn Balken war.
- Eine zweite Formel multipliziert die Average True Range mit dem Breakout-Multiplikator, und ein Vergleich fragt, ob die Spanne dieses Balkens über dieser Schwelle liegt — das ist die ganze Definition eines ungewöhnlich breiten Balkens.
- Die Richtung ist ein einziger Vergleich: Schluss über Eröffnung. Ein logisches Nicht macht aus demselben Signal den Fall des Abwärtsbalkens, sodass beide Einstiege denselben Kerzenkörper von entgegengesetzten Seiten lesen.
- Beide Einstiegsgatter sind ein logisches Und aus drei Bedingungen: Der Balken ist breit, er zeigt in die richtige Richtung, und die Position ist flat. Position modify kauft oder verkauft daraufhin zum Marktpreis mit dem Ordervolumen.
- P&L change liefert bei jeder Aktualisierung das offene Ergebnis der Position, und zwei Vergleiche messen es gegen das Geldziel und gegen den Geld-Stop.
- Das Geldziel ist keine feste Zahl: Eine Formel multipliziert das Basisziel mit einem Recovery-Faktor, der in einer Variablen gehalten wird, sodass sich das Ziel mit dem Recovery-Zustand bewegt, statt zweimal ins Diagramm eingetragen zu werden.
- Der Recovery-Faktor wird von genau zwei Ereignissen neu geschrieben, jedes über sein eigenes Gatter, und eine Combination führt die beiden Schreibvorgänge auf den einen Eingang der Variablen zusammen, die ihn speichert.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Eine abgeschlossene Kerze, deren Spanne von Hoch zu Tief die Average True Range mal dem Breakout-Multiplikator übersteigt und die über ihrer eigenen Eröffnung schließt — eingegangen bei flacher Position. Position modify kauft zum Marktpreis mit dem Ordervolumen.
- **Short-Einstieg**: Eine abgeschlossene Kerze, deren Spanne dieselbe Schwelle übersteigt, die aber nicht über ihrer eigenen Eröffnung schließt — eingegangen bei flacher Position. Position modify verkauft zum Marktpreis mit demselben Ordervolumen.
- **Ausstieg**: Im Diagramm gibt es weder einen Kurs-Stop noch ein Kursziel — die Position wird allein über Geld geschlossen. Erreicht das offene Ergebnis das aktuelle Geldziel, feuert ein Position modify, das auf das Schließen der Position gesetzt ist; fällt es auf den Geld-Stop, feuert ein zweites. Jeder schließende Block besitzt einen Zweig der Recovery-Verriegelung: Die Ausführung des Schlusses mit Verlust gibt den vergrößerten Faktor frei, die Ausführung des Schlusses mit Gewinn gibt den Wert eins frei, und beide Schreibvorgänge treffen sich in einer Combination, die die Variable mit dem Faktor speist.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:15:00 | Zeitrahmen der Kerzen, mit denen das gesamte Diagramm arbeitet. |
| ATR Length | 10 | Anzahl der Balken, über die die Average True Range gemessen wird; das ist der Maßstab, an dem ein breiter Balken beurteilt wird. |
| Breakout Multiplier | 1.5 | Wie viel breiter als der Durchschnitt ein Balken sein muss, damit er als Ausbruch zählt. Höher gesetzt für seltenere und extremere Einstiege, niedriger für mehr davon. |
| Volume | 1 | Größe jeder Einstiegsorder. Alle Geldbeträge weiter unten sind Ergebnisse dieser Größe, eine Änderung bedeutet also, die anderen neu abzustimmen. |
| Target Base | 300 | Geldziel eines Trades, der nach einem Gewinn-Trade eingegangen wird, in der Währung, in der das Ergebnis gezählt wird. |
| Recovery Multiplier | 2 | Womit das Ziel nach einem Verlust-Trade multipliziert wird. Zwei bedeutet, dass der nächste Trade das Doppelte des Basisziels zurückverdienen muss; eins schaltet die Recovery ab und lässt ein einfaches Geldziel übrig. |
| Stop Money | -600 | Offenes Ergebnis, bei dem eine Position aufgegeben wird, als negative Zahl geschrieben. Es ist ein fester Betrag und wird nicht mit dem Recovery-Faktor skaliert. |

## Diagrammdetails

- Die Verriegelung ist die einzige Schleife im Diagramm: Die Faktor-Variable speist eine Formel, die sie mit dem Recovery-Multiplikator multipliziert, das Ergebnis wartet in einer Gatter-Variablen, und das Gatter schreibt es in die Faktor-Variable zurück, sobald ein Schluss mit Verlust tatsächlich ausgeführt wird.
- Beide Gatter werden von der Ausführung eines schließenden Blocks ausgelöst, nicht von dem Vergleich, der das Schließen angefordert hat. Ein Vergleich kann sein Urteil mehrfach wiederholen, während die schließende Order noch unterwegs ist; eine Ausführung geschieht einmal, deshalb wird der Faktor je Verlust-Trade genau einmal multipliziert.
- Die Faktor-Variable nimmt ihren Eingang entgegen, ohne ihn als Auslöser zu behandeln, sodass ein Schreibvorgang nur ändert, was sie speichert. Sie gibt auf den Auslöser aus, den sie erhält, und das ist die P&L-Aktualisierung; damit laufen beide Seiten des Zielvergleichs auf derselben Uhr.
- Der Geldzweig bleibt still, bis die erste Position eröffnet ist, denn das offene Ergebnis wird erst gemeldet, wenn es etwas zu bewerten gibt. Von da an geben der Faktor, das Basisziel und der Recovery-Multiplikator bei jeder Aktualisierung gemeinsam frei.
- Die Kerzen werden nur als abgeschlossen abonniert, sodass jedes Signal zu einem bereits geschlossenen Balken gehört und die Orders die Zeit dieses Schlusses tragen und nicht die Zeit, zu der der Balken eröffnet wurde.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
