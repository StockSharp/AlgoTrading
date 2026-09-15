# Diagramm der Strategie Quote Momentum Sync
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum, gemessen von einem Schlusskurs zum nächsten. Das Diagramm vergleicht den Schlusskurs der soeben abgeschlossenen Kerze mit dem Schlusskurs davor und verlangt eine Bewegung von mindestens einer festen Anzahl von Kurseinheiten, und zwar in die Richtung, in die sich die vorangegangene Kerze bereits bewegt hat. Jeder Messwert wird im selben Fünf-Minuten-Takt genommen, sodass Referenzkurs, Richtungsfilter und Positionsprüfung stets im Gleichtakt sind und ein Signal nie aus Werten zusammengesetzt wird, die zu unterschiedlichen Zeitpunkten gehören.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen des gehandelten Instruments sind die einzigen Marktdaten, die das Diagramm abonniert, und alles Nachgelagerte läuft in diesem Takt.
- Previous value hält die gesamte vorangegangene Kerze und nicht nur eine einzelne Zahl; zwei Konverter lesen daraus deren Schlusskurs und deren Eröffnungskurs aus.
- Dieser vorherige Schlusskurs ist der Referenzkurs: Eine Formel addiert den Momentum-Schritt, eine andere zieht den Schritt ab, was ein Auslöseniveau für Long und eines für Short auf dem aktuellen Bar ergibt.
- Ein dritter Konverter liest den Schlusskurs der soeben abgeschlossenen Kerze, und zwei Vergleiche setzen ihn zu den beiden Niveaus ins Verhältnis.
- Der Richtungsfilter ist die Form der vorangegangenen Kerze: ihr Schlusskurs gegen ihren eigenen Eröffnungskurs, sodass ein bullischer Bar nur Longs und ein bärischer Bar nur Shorts zulässt.
- Ein Position-Block, mit null verglichen, sagt aus, ob das Konto flat ist; das hält das Diagramm aus einem bestehenden Trade heraus, statt ihn aufzustocken.
- Zwei logische Bedingungen fassen Richtung, Momentum und Flat-Zustand zusammen, und jede steuert ein Position modify, das mit festem Volumen per Market eröffnet und nur aus einer flachen Position heraus.
- Position protection übernimmt die Einstiegsausführungen und den aktuellen Schlusskurs und schließt den Trade bei einem prozentual festen Take-Profit oder Stop-Loss; einen anderen Ausstieg kennt das Diagramm nicht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die vorangegangene Kerze hat über ihrem eigenen Eröffnungskurs geschlossen, die soeben abgeschlossene Kerze hat über dem vorherigen Schlusskurs zuzüglich des Momentum-Schritts geschlossen, und die Position ist flat. Position modify kauft das Ordervolumen per Market.
- **Short-Einstieg**: Die vorangegangene Kerze hat unter ihrem eigenen Eröffnungskurs geschlossen, die soeben abgeschlossene Kerze hat unter dem vorherigen Schlusskurs abzüglich desselben Schritts geschlossen, und die Position ist flat. Position modify verkauft das Ordervolumen per Market.
- **Ausstieg**: Im Diagramm gibt es kein Ausstiegssignal. Vom Moment der Ausführung eines Einstiegs an gehört der Trade zu Position protection, das ihn bei 0.5% Gewinn oder 0.5% Verlust gegenüber dem Einstiegskurs schließt. Die Absicherung wird vom Kerzenschlusskurs aus bepreist, die Niveaus werden also einmal pro Bar geprüft, und ein gegenläufiger Messwert wird ignoriert, solange eine Position offen ist: Der nächste Einstieg wartet, bis die Position wieder flat ist.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzenserie, in deren Takt das gesamte Diagramm läuft. |
| Momentum Step | 5 | Abstand vom vorherigen Schlusskurs, in Kurseinheiten, den die abgeschlossene Kerze überwinden muss, bevor ein Einstieg zugelassen wird. |
| Order Volume | 0.01 | Ordergröße, in Lots. |
| Take Profit, % | 0.5 | Take-Profit-Abstand, in Prozent des Einstiegskurses. |
| Stop Loss, % | 0.5 | Stop-Loss-Abstand, in Prozent des Einstiegskurses. |

## Diagrammdetails

- Previous value ist so eingestellt, dass es eine Kerze hält und keine Zahl; die Felder werden anschließend von Konvertern ausgelesen. Genau dadurch stammen Referenzkurs und Richtungsfilter aus ein und demselben Bar.
- Die Einstiegsbedingung ist als zwei Kursniveaus formuliert und nicht als Differenz gegen einen Schwellenwert — dieselbe Arithmetik von der anderen Seite betrachtet, die es dem Chart erlaubt, das Niveau anzuzeigen, aus dem der jeweilige Einstieg hervorgegangen ist.
- Beide Einstiegsblöcke eröffnen nur aus einer flachen Position, sodass wiederholte Signale innerhalb eines laufenden Trades folgenlos bleiben und keine Order zum Drehen oder zum Aufstocken gesendet wird.
- Jede Konstante wird von der Kerzenserie ausgelöst und veröffentlicht ihren Wert daher auf jedem Bar; eine Konstante, die nie auslöst, ließe ihren Vergleich ohne zweiten Operanden und die Bedingung käme nie zustande.
- Der Momentum-Schritt ist ein absoluter Abstand in den Einheiten des Kurses selbst und kein Prozentwert; für ein Instrument, das in einer anderen Größenordnung notiert, muss er daher umskaliert werden, während Take-Profit und Stop-Loss Prozentwerte sind und unverändert übertragbar bleiben.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
