# Strategiediagramm mit zufälliger Positionsgröße und wechselnden Einstiegen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Jedes Diagramm der Galerie entscheidet, mit welchem Volumen gehandelt wird; dieses verweigert die Entscheidung. Der Random-Block zieht auf jeder Kerze eine neue Größe zwischen einem halben und zwei Lot und legt sie direkt an den Volumen-Eingang beider Einstiegsblöcke an, sodass keine zwei Positionen gleich groß sind. Die Richtung ist dagegen keineswegs zufällig: Sie ergibt sich daraus, wo der letzte gehandelte Preis gegenüber der Eröffnung der laufenden Kerze steht.

![schema](schema.svg)

## Strategieübersicht

- Das Signal liefert der Tickstrom. Ein Konverter liest den Preis des zuletzt ausgeführten Trades, und eine Variable hält ihn bis zum Schluss der Kerze — erst dadurch laufen Ausführungspreis und Kerzeneröffnung im selben Takt.
- Ein Vergleich prüft, ob dieser Preis über der Kerzeneröffnung liegt. Dasselbe Ergebnis, durch ein logisches NICHT invertiert, ergibt die Short-Seite; ein einziger Vergleich bedient somit beide Richtungen.
- Der Random-Block wird von der Kerze ausgelöst und übergibt seine Zahl an den Volumen-Eingang des Kauf- und des Verkaufsblocks. Sonst liest sie niemand im Diagramm.
- Eingestiegen wird nur aus einer flachen Position heraus; beide Blöcke tragen die Bedingung auf die offene Position, sodass ein Signal eine bereits offene Position nicht aufstocken kann.
- Ein Zähler der Kerzen seit der letzten Ausführung hält zwölf Kerzen Abstand zwischen den Einstiegen. Ohne ihn würden sich Schutzausstieg und nächster Einstieg auf aufeinanderfolgenden Kerzen jagen.
- Position protection ist der einzige Ausstieg. Der Block erhält die Einstiegsausführungen über eine Combination, bewertet sie am Schlusskurs der Kerze und schließt die Position bei einem Ziel von 0.4% oder einem Trailing-Stop von 0.5%.
- Der Stop zieht nach, sodass eine Position, die in die richtige Richtung läuft, nur den letzten Teil der Bewegung wieder abgibt.
- Das Chartfenster zeichnet die Kerzen, beide Orderströme und jede Ausführung, auch die der Schutzorders.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der letzte gehandelte Preis liegt über der Eröffnung der laufenden Kerze, die Position ist flach, und seit der letzten Ausführung sind zwölf Kerzen vergangen. Position modify kauft zum Marktpreis — mit dem Volumen, das der Random-Block für diese Kerze gezogen hat.
- **Short-Einstieg**: Der letzte gehandelte Preis liegt auf oder unter der Eröffnung der laufenden Kerze, bei denselben Bedingungen aus flacher Position und Wartezeit. Position modify verkauft zum Marktpreis mit demselben zufällig gezogenen Volumen.
- **Ausstieg**: Ein Ausstiegssignal gibt es nicht. Position protection übernimmt die Position ab der ersten Ausführung und schließt sie bei einem Take-Profit von 0.4% oder einem Trailing-Stop von 0.5%, jeweils gemessen am Einstiegspreis. Jede Ausführung, auch die der Schutzorders, startet die Wartezeit von zwölf Kerzen bis zum nächsten Einstieg neu.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzen, auf denen das gesamte Diagramm arbeitet. |
| Cooldown Bars | 12 | Wie viele abgeschlossene Kerzen nach einer Ausführung vergehen müssen, bevor der nächste Einstieg erlaubt ist. |
| Min Volume | 0.5 | Kleinste Größe, die der Random-Block ziehen darf. |
| Max Volume | 2 | Größte Größe, die der Random-Block ziehen darf. |
| Take Profit, % | 0.4 | Take-Profit-Abstand, in Prozent des Einstiegspreises. |
| Stop Loss, % | 0.5 | Trailing-Stop-Abstand, in Prozent des Einstiegspreises. |

## Diagrammdetails

- Der Kerzenblock speist acht Abnehmer: beide Konverter, die Variable mit dem Ausführungspreis, den Random-Block, den Wartezeit-Zähler samt seinen beiden Variablen und das Chartfenster.
- Die Variable mit dem Ausführungspreis ist das Einzige, was zwischen einem Tickstrom, der tausende Male am Tag feuert, und einer Bedingung steht, die pro Kerze nur einmal beantwortet werden soll.
- Beide Einstiegsblöcke teilen sich einen Random-Ausgang, Long und Short werden also aus derselben Ziehung dimensioniert; die Zahl wechselt mit der nächsten Kerze, nicht zwischen den beiden Blöcken.
- Der Wartezeit-Zähler wird vom Block der Strategieausführungen zurückgesetzt; deshalb startet auch ein Schutzausstieg die Wartezeit, nicht nur ein Einstieg.
- Die Größe wird mit zwei Nachkommastellen gezogen, was zu einem Instrument passt, das in Bruchteilen einer Einheit notiert wird; bei einem Instrument mit ganzen Lots wird der Bereich stattdessen in ganzen Zahlen gesetzt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
