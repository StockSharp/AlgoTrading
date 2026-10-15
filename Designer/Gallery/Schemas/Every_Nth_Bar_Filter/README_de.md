# Diagramm der Strategie „Every Nth Bar Filter“
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei exponentielle gleitende Durchschnitte kreuzen sich, und die eine Seite des Marktes wird gekauft, während die andere verkauft wird. Der Kern dieses Diagramms ist das, womit die Durchschnitte gespeist werden. Statt jede Kerze zu lesen, lesen sie einen Preis aus jeder fünften Kerze, und die Ausdünnung übernimmt ein N values-Block, der den Kerzenstrom gegen sich selbst zählt. Alles Nachgelagerte — die Durchschnitte, die Kreuzung, die Einstiege — lebt auf diesem langsameren Takt, sodass das Diagramm den Markt einmal je Abtastfenster betrachtet und nicht einmal je Bar.

![schema](schema.svg)

## Strategieübersicht

- Fertige Fünf-Minuten-Kerzen speisen einen Konverter, der aus jeder Kerze den Schlusskurs zieht, sowie einen N values-Block, der denselben Strom in einen Abtastimpuls verwandelt.
- Der N values-Block nimmt den Kerzenstrom gleichzeitig an beiden Eingängen entgegen: Der Trigger schärft seinen Countdown, der Eingang zählt ihn herunter, sodass er bei jeder fünften fertigen Kerze einen Impuls abgibt und sich sofort wieder schärft.
- Dieser Impuls ist der Trigger einer Variable, die den jeweils letzten Schlusskurs hält. Die Variable speichert jeden Schlusskurs, sobald er eintrifft, gibt aber nichts frei, bis der Impuls kommt; was sie verlässt, ist deshalb eine ausgedünnte Preisreihe — ein Wert je Abtastfenster.
- Beide gleitenden Durchschnitte lesen diese ausgedünnte Reihe statt der Kerzen, sodass ein Durchschnitt über vierzehn Perioden siebzig Kerzen Marktzeit umspannt und einer über vierzig Perioden zweihundert.
- Ein Crossing-Block beobachtet den schnellen Durchschnitt gegenüber dem langsamen und meldet sich nur in dem Moment, in dem die beiden die Plätze tauschen: true, wenn der schnelle Durchschnitt nach oben kreuzt, false, wenn er nach unten kreuzt. Ein logisches NOT macht aus dem Abwärtsfall ein eigenes Signal.
- Der Position-Block wird über zwei Vergleiche gegen eine Null-Variable geprüft — nicht long und nicht short — und jedes Ergebnis wird per logischem AND mit seinem Kreuzungssignal verknüpft, sodass ein Einstieg eine frische Kreuzung und eine Position verlangt, die nicht bereits auf dieser Seite steht.
- Beide Einstiegsblöcke sind auf „nur eröffnen“ gesetzt, sodass das Diagramm jeweils nur eine Position führt und niemals aufstockt; das Ordervolumen stammt aus einer Variable, die bei jeder Kerze aufgefrischt wird.
- Die entgegengesetzte Kreuzung steuert zwei Schließen-Blöcke, Position protection wird von jeder Ausführung geschärft, und das Chart-Panel zeichnet die Kerzen, beide Durchschnitte, die Einstiegs- und Ausstiegsorders, die Schutzorders und jede Ausführung.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der schnelle Durchschnitt kreuzt den langsamen auf einem Abtastimpuls nach oben, und die Position ist nicht long. Position modify kauft das Ordervolumen zum Marktpreis, nur eröffnend, sodass der Einstieg aus einem positionslosen Konto heraus erfolgt und sich nie auf einen offenen Trade stapelt.
- **Short-Einstieg**: Der schnelle Durchschnitt kreuzt den langsamen auf einem Abtastimpuls nach unten, und die Position ist nicht short. Das logische NOT macht aus der Abwärtskreuzung ein Signal, und Position modify verkauft das Ordervolumen zum Marktpreis, nur eröffnend.
- **Ausstieg**: Es gibt zwei Wege hinaus. Position protection, von jeder Ausführung geschärft, schließt den Trade bei 1.5% Gewinn oder an einem Stop von 1%. Wird keines von beiden erreicht, bevor die Durchschnitte zurücktauschen, übernimmt die entgegengesetzte Kreuzung: Sie löst den Schließen-Block für die gehaltene Seite aus, und dieser Block schickt die gesamte Position zum Marktpreis. Da die Einstiegsblöcke nur eröffnen, öffnet die Kreuzung, die einen Trade beendet, nicht die Gegenposition — der nächste Einstieg wartet auf die nächste Kreuzung, die auf ein positionsloses Konto trifft.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzen, auf dem das gesamte Diagramm arbeitet; das Abtastfenster wird in diesen Kerzen gezählt. |
| Bars Per Sample | 5 | Wie viele fertige Kerzen eine Abtastung ergeben. Erhöht man den Wert, sehen die Durchschnitte den Markt seltener und handeln seltener; setzt man ihn auf eins, wird das Diagramm zu einem gewöhnlichen Crossover auf jeder Kerze. |
| Fast EMA Length | 14 | Länge des schnellen Durchschnitts, gezählt in Abtastungen statt in Kerzen: bei fünf Kerzen je Abtastung deckt er die fünffache Anzahl an Kerzen Marktzeit ab. |
| Slow EMA Length | 40 | Länge des langsamen Durchschnitts, in Abtastungen. Halten Sie deutlichen Abstand zur schnellen Länge, sonst tauschen die beiden Linien schon auf Rauschen die Plätze und die Kreuzungen sagen nichts mehr aus. |
| Order Volume | 1 | Größe jeder Einstiegsorder, in Instrumenteinheiten. Die Schließen-Blöcke ignorieren sie und schicken das, was die Position hält. |
| Take Profit, % | 1.5 | Take-Profit-Abstand, in Prozent des Ausführungspreises. |
| Stop Loss, % | 1 | Stop-Loss-Abstand, in Prozent des Ausführungspreises. |

## Diagrammdetails

- Der N values-Block dient hier als Ausdünner, nicht als Verzögerung. Beide Eingänge kommen aus demselben Kerzenstrom, sodass der Countdown in dem Moment neu startet, in dem er abläuft, und der Impuls über den ganzen Lauf hinweg auf jeder fünften fertigen Kerze landet.
- Die Variable zwischen Impuls und Durchschnitten macht die Neuabtastung erst wirklich: Ihr Eingang nimmt jeden Schlusskurs an, ihr Ausgang bleibt stumm, bis der Trigger eintrifft, und so bekommen die Durchschnitte einen Wert je Fenster und sehen die Kerzen dazwischen nie.
- Beide Durchschnitte lesen dieselbe Variable, laufen dadurch im Gleichtakt, und der Crossing-Block kann ihre Werte paaren, sobald sie eintreffen. Er meldet nur die Abtastung, bei der sich die Reihenfolge der beiden Linien geändert hat — deshalb sind Einstiege in den Kerzen zwischen zwei Abtastungen unmöglich.
- Die Positionsprüfungen sind als nicht long und nicht short formuliert und nicht als positionslos, sodass eine Kreuzung, die eintrifft, während die Gegenseite noch offen ist, den Einstiegsblock dennoch erreicht; dass das Diagramm bei einer einzigen Position bleibt, dafür sorgt die Einstellung „nur eröffnen“, und die Schließen-Blöcke geben sie wieder frei.
- Jede Ausführung, Einstiege wie Ausstiege, wird von einem Combination-Block zusammengeführt und an Position protection geschickt, sodass die Schutzseite stets die Position sieht, die das Konto tatsächlich hält, und abtritt, sobald eine Kreuzung den Trade geschlossen hat.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
