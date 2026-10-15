# Diagramm der Strategie zur Spread-Abweichung zweier Instrumente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei Instrumente, die sich für gewöhnlich gemeinsam bewegen, tun dies gelegentlich nicht, und der Abstand zwischen ihnen schließt sich in der Regel wieder. Das Diagramm dividiert den einen Preis durch den anderen, beobachtet, wie weit sich dieses Verhältnis von seinem eigenen gleitenden Durchschnitt entfernt, und kauft oder verkauft das erste Instrument in dem Moment, in dem sich der Abstand zu schließen beginnt, und nicht in dem Moment, in dem er sich öffnet.

![schema](schema.svg)

## Strategieübersicht

- Ein Index-Baustein bildet aus zwei realen Instrumenten ein synthetisches Instrument, indem er den Preis des ersten durch den Preis des zweiten dividiert; auf dieses synthetische Instrument wird eine Kerzenreihe abonniert, sodass der Spread als fertige Kerzen eintrifft, statt von Hand zusammengesetzt zu werden.
- Ein gleitender Durchschnitt läuft über die Spread-Kerzen, und ein Konverter nimmt deren Schlusskurs; damit hat der Spread sowohl ein aktuelles Niveau als auch eine eigene Referenzlinie.
- Eine Formel reduziert das Paar auf eine einzige Zahl: den Abstand vom Schlusskurs zum Durchschnitt, in Prozent des Durchschnitts.
- Derselbe Messwert eine Bar zuvor wird aus einer vorherigen Spread-Kerze und einem vorherigen Durchschnittswert neu aufgebaut, und eine zweite Formel macht aus diesen beiden die Abweichung der vorherigen Bar.
- Eine zweite Kerzenreihe läuft auf dem gehandelten Instrument, und zwei Variablen klinken die beiden Abweichungen darauf ein: Jede hält die letzte Zahl fest, die der Spread geliefert hat, und gibt sie frei, sobald eine Kerze des gehandelten Instruments abgeschlossen ist. So wird jede Entscheidung nach der Uhr des Instruments getroffen, an das die Orders gehen.
- Vergleiche stellen dann vier Fragen: wo die vorherige Abweichung im Verhältnis zur Schwelle stand, in welche Richtung sich die Abweichung jetzt bewegt, auf welcher Seite des Durchschnitts sie noch steht und ob keine Position offen ist. Eine logische Bedingung fasst die vier Antworten zu einem Einstiegstor je Richtung zusammen.
- Einstiege sind Market-Orders fester Größe, die nur eingegangen werden, wenn keine Position offen ist, und sie werden für das Instrument gesendet, auf das die Strategie selbst eingestellt ist. Das synthetische Instrument ist eine Quelle von Zahlen und trägt niemals eine Order.
- Zwei weitere Vergleiche beobachten, wie die Abweichung den Durchschnitt wieder erreicht, und übergeben diesen Moment an zwei Position-Bausteine, einen je Seite, die schließen, was offen ist.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Abweichung der vorherigen Bar lag unter der unteren Schwelle, die Abweichung dieser Bar ist höher als die vorherige, die Abweichung liegt weiterhin unter dem Durchschnitt, und es ist keine Position offen. Der Long-Baustein kauft das Ordervolumen zum Marktpreis.
- **Short-Einstieg**: Die Abweichung der vorherigen Bar lag über der oberen Schwelle, die Abweichung dieser Bar ist niedriger als die vorherige, die Abweichung liegt weiterhin über dem Durchschnitt, und es ist keine Position offen. Der Short-Baustein verkauft das Ordervolumen zum Marktpreis.
- **Ausstieg**: Eine Position wird geschlossen, wenn der Spread den Weg beendet, für den er gekauft wurde: ein Long, sobald die Abweichung den Durchschnitt erreicht oder darüber hinausgeht, ein Short, sobald sie ihn erreicht oder darunter fällt. Jeder schließende Baustein trägt eine eigene Seite, sodass der für Longs bestimmte keinen Short berühren kann und ein schließender Baustein, der ohne offene Position auslöst, einfach nichts tut. Es gibt hier kein Gewinnziel, keinen Stop-Loss und keine Zeitbegrenzung; die Rückkehr des Spreads ist der gesamte Ausstieg.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | Der Ausdruck, aus dem das synthetische Instrument gebildet wird: der Preis des ersten Instruments dividiert durch den Preis des zweiten. Beide Instrumente müssen in den verbundenen Daten vorhanden sein, und nur das erste von ihnen wird gehandelt. |
| Spread Candles | 00:05:00 | Länge der Kerzen, auf denen die Spread-Reihe aufgebaut wird. |
| Traded Candles | 00:05:00 | Länge der Kerzen, auf denen die Orders platziert werden. Halten Sie sie gleich der Spread-Reihe, sonst sind die eingeklinkten Zahlen älter als die Bar, auf der sie gelesen werden. |
| Average Length | 20 | Anzahl der Spread-Kerzen in dem gleitenden Durchschnitt, von dem aus die Abweichung gemessen wird. |
| Deviation Threshold, % | 0.3 | Wie weit sich der Spread in Prozent von seinem Durchschnitt entfernen muss, bevor eine Rückkehr in dessen Richtung einen Trade wert ist. |
| Order Volume | 1 | Ordergröße in Lots. |

## Diagrammdetails

- Die Abweichung der vorherigen Bar wird aus einer vorherigen Kerze und einem vorherigen Durchschnittswert neu aufgebaut, statt den fertigen Prozentwert festzuhalten. So werden beide Hälften des Verhältnisses eine Bar zurück gelesen, und der Vergleich von Jetzt gegen Damals kann nicht zwei verschiedene Zeitpunkte vermischen.
- Die beiden Reihen enden nicht im selben Augenblick: Ein synthetisches Instrument wird aus zwei Datenströmen zusammengesetzt, und seine Kerze schließt etwas später als die einfache. Das Einklinken der Zahlen auf der Kerze des gehandelten Instruments hält die Strategie auf einer einzigen Uhr; gäbe man stattdessen beide Reihen gemeinsam frei, trüge jede Order den früheren der beiden Zeitpunkte, und eine Order, die vor der aktuellen Zeit datiert ist, wird abgelehnt.
- Die Konstanten werden von der Kerze des gehandelten Instruments ausgelöst. Ein Vergleich braucht für jede Auswertung beide Werte erneut, sodass eine Konstante, die nicht erneut gesendet wird, die von ihr gespeiste Bedingung anhält, ohne dass davon etwas zu sehen wäre.
- Die untere Schwelle ist keine zweite Konstante, sondern eine Formel über demselben Wert, sodass das Band symmetrisch bleibt, worauf die Schwelle auch gesetzt wird.
- Den Order-Bausteinen ist vorgegeben, nicht darauf zu warten, dass die Strategie online geht, und die Einstiege sind so eingestellt, dass sie nur eröffnen, wenn keine Position offen ist; ein Signal, das sich während eines laufenden Trades wiederholt, fügt ihm daher nichts hinzu.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
