# Diagramm der Tick-Spike-Fade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Spike ist im Schlusskurs nicht zu sehen. Der Kurs kann ein halbes Prozent von dort weglaufen, wo er vor zwanzig Minuten stand, und noch vor Ablauf der Minute wieder zurück sein, und die Kerze, die das aufzeichnet, sieht aus wie jede andere. Dieses Diagramm beobachtet stattdessen den Tickstrom: Jeder ausgeführte Tick wird gegen den Schlusskurs von vor zwanzig Bars gemessen, und der erste Tick, der weit genug entfernt liegt, schärft eine Seite des Trades. Die Order geht dann in die Gegenrichtung — ein Anstieg wird verkauft, ein Rückgang gekauft — und sie wird zum Schluss der Bar gesendet, in der der Spike aufgetreten ist.

![schema](schema.svg)

## Strategieübersicht

- Eine Ein-Minuten-Kerzenreihe ist die Uhr des Diagramms. Weitergegeben werden nur abgeschlossene Kerzen, daher werden der Referenzkurs, der Freigabe-Timer und der Moment, in dem ein scharfgeschaltetes Signal zur Order wird, allesamt in geschlossenen Bars gezählt.
- Neben den Kerzen wird der Tickstrom abonniert, und ein Konverter liest den Kurs aus jedem ausgeführten Tick. Dieser Tickkurs, nicht ein Kerzenschluss, ist der aktuelle Kurs des gesamten Diagramms.
- Ein Previous value-Block hält die Kerze von vor zwanzig Bars, und ein Konverter entnimmt deren Schlusskurs. Das ist die Referenz, gegen die der aktuelle Kurs gemessen wird, weit genug entfernt, dass eine einzelne Minute Rauschen die Schwelle nicht erreichen kann.
- Eine Variable hält diese Referenz und gibt sie bei jedem Tick frei, sodass die nachfolgende Formel beide Eingänge im Takt des Tickstroms aktualisiert bekommt. Sie ermittelt den Abstand zwischen dem letzten Tick und dem Referenzschluss in Prozent und ist so abgesichert, dass eine Referenz von null überhaupt kein Ergebnis erzeugt.
- Eine zweite Formel negiert diesen Prozentwert, wodurch eine einzige Schwellenkonstante beide Richtungen bedient: Der Vergleich mit ihr beantwortet den Anstieg, derselbe Vergleich am negierten Wert beantwortet den Rückgang.
- Jede Richtung besitzt ein eigenes Flag. Der erste Tick, der die Schwelle überschreitet, setzt sein Flag, und das Flag gibt einen einzelnen Impuls aus; jeder spätere Tick derselben Bewegung wird ignoriert, sodass ein Spike eine Entscheidung erzeugt und nicht hundert.
- Dieser Impuls schärft einen auf einen Wert gestellten N values-Block, der ihn mit der nächsten abgeschlossenen Kerze freigibt. Die Entscheidung fällt zwischen den Kerzen, im Tickstrom, und die Order wird auf einer Kerze gesendet.
- Position modify eröffnet per Market-Order unter der Bedingung zum Eröffnen einer Position, sodass ein Diagramm, das bereits etwas hält, niemals aufstockt. Anschließend übernimmt Position protection den Trade, und dessen Price-Eingang wird ebenfalls aus dem Tickstrom gespeist.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der letzte ausgeführte Tick liegt um die Schwelle oder mehr unter dem Schlusskurs von vor zwanzig Bars, und das Flag für den Rückgang ist frei. Das Flag feuert seinen einzelnen Impuls, der Ein-Wert-Halt gibt ihn zum Schluss der laufenden Bar frei, und Position modify kauft das Ordervolumen per Market-Order.
- **Short-Einstieg**: Der letzte ausgeführte Tick liegt um die Schwelle oder mehr über dem Schlusskurs von vor zwanzig Bars, und das Flag für den Anstieg ist frei. Das Flag feuert seinen einzelnen Impuls, und zum Schluss der Bar, in der der Spike aufgetreten ist, verkauft Position modify das Ordervolumen per Market-Order.
- **Ausstieg**: Es gibt kein Ausstiegssignal und keine eigene Schließregel. Position protection übernimmt die Einstiegsausführung und setzt einen Take-Profit 0.6% und einen Stop-Loss 0.3% vom Ausführungskurs entfernt; da sein Price-Eingang aus dem Tickstrom gespeist wird, werden beide Marken bei jedem ausgeführten Tick geprüft statt einmal pro Minute. Was zuerst berührt wird, schließt die Position mit einer Market-Order, und das Diagramm ist wieder flat, lange bevor der Freigabe-Timer abgelaufen ist.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:01:00 | Zeitrahmen der Kerzenreihe. Sie ist die Uhr des Diagramms: Der Referenzschluss, der Freigabe-Timer und der Moment, in dem ein scharfgeschaltetes Signal zur Order wird, werden allesamt in diesen Kerzen gezählt. |
| Lookback Bars | 20 | Wie weit zurück der Referenzschluss genommen wird, in Kerzen. Ein größerer Wert misst die Bewegung über ein längeres Fenster und macht dieselbe Schwelle schwerer erreichbar. |
| Spike Threshold, % | 0.3 | Wie weit der letzte Tick vom Referenzschluss entfernt stehen muss, in Prozent, damit die Bewegung als Spike zählt. Ein Wert bedient beide Richtungen. |
| Cooldown Bars | 30 | Wie viele abgeschlossene Kerzen nach einem Spike vergehen, bevor die Flags freigegeben werden und das Diagramm wieder reagieren darf. |
| Order Volume | 1 | Ordergröße, in Lots. |
| Take Profit, % | 0.6 | Take-Profit-Abstand vom Ausführungskurs, in Prozent, geprüft bei jedem ausgeführten Tick. |
| Stop Loss, % | 0.3 | Stop-Loss-Abstand vom Ausführungskurs, in Prozent, geprüft bei jedem ausgeführten Tick. |

## Diagrammdetails

- Erst das Ablesen des aktuellen Kurses aus dem Tickstrom macht aus dem Vergleich einen Spike-Detektor. Eine Bewegung, die ein Drittel Prozent wegläuft und innerhalb derselben Minute zurückkommt, hinterlässt im Schlusskurs kaum eine Spur, doch jeder ihrer Ticks durchläuft den Vergleich, und der erste jenseits der Linie setzt das Flag.
- Die Referenz ist ein Schlusskurs von vor zwanzig Bars und nicht der vorletzte Schlusskurs. Über eine Minute ist selbst eine heftige Bewegung klein, und eine Schwelle, die niedrig genug wäre, um darauf zu reagieren, würde schon bei gewöhnlichem Rauschen auslösen.
- Die Flags sind es, die aus einem Spike genau einen Trade machen. Ein Flag bleibt gesetzt, bis der Freigabe-Timer dreißig abgeschlossene Kerzen gezählt hat, sodass eine Bewegung, die sich weiter ausdehnt, auf ihrem Weg nach oben nicht dreimal verkauft werden kann.
- Die Order wird von einer Kerze getaktet, obwohl die Entscheidung an einem Tick fiel: Der Ein-Wert-Halt reicht das scharfgeschaltete Signal an die erste danach abgeschlossene Kerze weiter, und die Einstiegsorder trägt die Zeit dieser Kerze.
- Beide Schutzabstände sind Prozentwerte des Ausführungskurses und keine festen Schritte, sodass dieselben Zahlen bei einem Instrument, das nahe 65 000 notiert, dasselbe bedeuten wie bei einem, das nahe 5 notiert.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
