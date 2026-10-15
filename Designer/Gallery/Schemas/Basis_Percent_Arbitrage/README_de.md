# Diagramm der Basis-Prozent-Arbitrage-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Paar hat zwei Preise: den, den es wert ist, und den, zu dem es gehandelt werden kann. Das Diagramm bildet den ersten als synthetisches Instrument und dessen gleitenden Durchschnitt ab, liest den zweiten aus den beiden Orderbüchern, drückt den Abstand zwischen beiden in Prozent aus und eröffnet beide Legs, sobald dieser Abstand über eine Schwelle hinauswächst.

![schema](schema.svg)

## Strategieübersicht

- Ein Index-Block bildet ein einziges synthetisches Instrument, indem er den Preis des ersten Instruments durch den Preis des zweiten teilt; darauf wird eine Kerzenserie abonniert, und ein gleitender Durchschnitt über diese Kerzen ist der faire Wert des Paares.
- Zwei Orderbuch-Blöcke liefern die Preise, zu denen das Paar tatsächlich gehandelt werden kann: Ein Konverter nimmt den besten Geldkurs des ersten Instruments, ein weiterer den besten Briefkurs des zweiten.
- Ein Orderbuch ändert sich innerhalb einer einzigen Bar viele Male, deshalb werden die beiden besten Preise nicht direkt in die Bedingungen geführt: Zwei Variablen halten jeweils die letzte Quotierung fest und geben sie frei, sobald eine gehandelte Kerze abgeschlossen ist, und eine Formel teilt die eine durch die andere zum ausführbaren Verhältnis.
- Ein Sync-Block hält das ausführbare Verhältnis und die Ratio-Kerze desselben Zeitstempels zurück und lässt beide gemeinsam heraus, sodass der faire Wert und der handelbare Preis, die verglichen werden, immer zur selben Bar gehören.
- Eine Formel macht aus dem freigegebenen Paar eine einzige Zahl: den Abstand vom ausführbaren Verhältnis zum fairen Durchschnitt, in Prozent des Durchschnitts. Diese Zahl ist die Basis.
- Eine Variable koppelt die Basis an die gehandelte Kerze, und jeder nachgelagerte Vergleich wird auf dieser Bar ausgewertet, sodass jede Order auf die Bar datiert ist, auf der sie entschieden wurde.
- Die Basis wird mit der Einstiegsschwelle und mit derselben negierten Schwelle verglichen, was ein Gate je Richtung ergibt, und mit einem deutlich engeren Band um den fairen Wert, dessen zwei Antworten eine logische Bedingung zum Rückkehrsignal zusammenführt.
- Ein Delay-Block, den einer der beiden Einstiege scharf schaltet, zählt abgeschlossene gehandelte Bars und setzt ein einzelnes Flag, sobald die Haltegrenze erreicht ist; eine Combination führt dieses Flag mit dem Rückkehrsignal zu einem einzigen Ausstiegsstrom zusammen, und zwei Schließ-Blöcke bauen das Paar ab, je einer pro Instrument.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Das ausführbare Verhältnis liegt um mehr als die Mindestbasis unter dem fairen Durchschnitt: Das Diagramm kauft das erste Instrument und verkauft das zweite, mit einem Ordervolumen je Leg. Jedes Leg ist so eingestellt, dass es nur aus einer eigenen flachen Position heraus eröffnet, sodass ein Signal, das sich wiederholt, während das Paar bereits offen ist, nichts hinzufügt.
- **Short-Einstieg**: Das ausführbare Verhältnis liegt um mehr als die Mindestbasis über dem fairen Durchschnitt: Das Diagramm verkauft das erste Instrument und kauft das zweite, mit einem Ordervolumen je Leg. Dieselbe Bedingung auf eine flache Position sichert jedes Leg einzeln ab.
- **Ausstieg**: Beide Legs werden abgebaut, sobald eines von beidem zuerst eintritt: Die Basis kehrt in das enge Band um den fairen Wert zurück, oder der Delay-Block hat seit dem Einstieg, der ihn scharf geschaltet hat, die Haltegrenze an abgeschlossenen gehandelten Bars gezählt. Beide Gründe erreichen denselben Auslöser über einen einzigen zusammenführenden Block. Die Schließ-Blöcke tragen keine Richtung und ermitteln ihr Volumen selbst aus dem, was offen ist, und ein Schließ-Block, der auf einem flachen Leg auslöst, tut schlicht nichts.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Traded Candles | 00:05:00 | Länge der Kerzen, auf denen die Entscheidungen getroffen und die Orders platziert werden. |
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | Der Ausdruck, aus dem das synthetische Instrument gebildet wird: der Preis des ersten Instruments geteilt durch den Preis des zweiten. Beide Instrumente müssen in den verbundenen Daten vorhanden sein, und beide werden gehandelt. |
| Ratio Candles | 00:05:00 | Länge der Kerzen, auf denen der faire Wert gebildet wird. Halte sie gleich der gehandelten Serie, sonst treffen die beiden nicht in einem Satz zusammen. |
| Sync Interval | 00:05:00 | Bucket, nach dem der Sync-Block sammelt. Halte ihn gleich der Kerzenlänge: Ein kürzerer Bucket trennt das Wertepaar, das zusammengehört, ein längerer führt Werte aus verschiedenen Bars zusammen. |
| Fair Average Length | 20 | Anzahl der Ratio-Kerzen in dem gleitenden Durchschnitt, aus dem der faire Wert genommen wird. |
| Minimum Basis, % | 0.5 | Wie weit das handelbare Verhältnis vom fairen Wert entfernt stehen muss, in Prozent, bevor beide Legs eröffnet werden. |
| Exit Basis, % | 0.1 | Wie nah die Basis wieder an den fairen Wert herankommen muss, in Prozent, bevor das Paar abgebaut wird. |
| Volume Per Leg | 1 | Ordergröße jedes Legs, in Lots. Beide Legs werden mit derselben Größe gesendet. |
| Max Hold Bars | 72 | Wie viele abgeschlossene gehandelte Bars das Paar gehalten werden darf, bevor es unabhängig von der Basis geschlossen wird. |

## Diagrammdetails

- Das Orderbuch wird nie direkt in eine Bedingung eingelesen. Es aktualisiert sich viele Male je Bar, während die Bedingungen auf der Bar leben; die beiden haltenden Variablen sind es, die beides auf eine Uhr bringen, und ihr Auslöser ist die gehandelte Kerze und nicht das Orderbuch.
- Nichts, was eine Order platziert, wird von einem Sync-Ausgang ausgelöst. Ein freigegebener Satz ist auf das früheste seiner Mitglieder datiert, und eine Order, die vor der aktuellen Zeit datiert ist, wird abgelehnt; deshalb speist Sync die Arithmetik, während die gehandelte Kerze jeden Auslöser antreibt.
- Beide Kerzenserien sind auf ausschließlich abgeschlossene Kerzen eingestellt. Ein Update einer noch entstehenden Bar würde eine Order auf den Moment datieren, in dem die Bar geöffnet wurde, und der liegt bereits in der Vergangenheit, wenn über die Bar entschieden wird.
- Die Konstanten werden bei jeder gehandelten Kerze erneut gesendet. Ein Vergleich braucht für jede Auswertung beide seiner Werte erneut, sodass eine nur einmal gesendete Konstante die von ihr gespeiste Bedingung stillschweigend zum Erliegen bringt.
- Die unteren Schwellen sind keine eigenen Konstanten, sondern Formeln über den oberen, sodass beide Bänder symmetrisch bleiben, worauf die Schwellen auch gesetzt werden, und die Haltegrenze wird in abgeschlossenen Bars gezählt statt in Uhrzeit.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
