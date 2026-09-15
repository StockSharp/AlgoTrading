# Strategiediagramm: EMA-Kreuzung mit Trailing-Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Einstieg in diesem Diagramm ist das Schlichteste aus der Palette: ein Schlusskurs, der auf Vier-Stunden-Kerzen einen exponentiellen Durchschnitt kreuzt. Worum es im Beispiel eigentlich geht, ist der Block, der die Position danach wieder abbaut. Position protection wird durch die Ausführung des Einstiegs scharf geschaltet, bekommt bei jeder geschlossenen Kerze einen Preis übergeben und zieht bei eingeschaltetem Trailing den Stop hinter einem Trade her, der in die eigene Richtung läuft, sodass der Ausstieg eine nachgezogene Marke ist statt einer beim Einstieg fixierten Linie.

![schema](schema.svg)

## Strategieübersicht

- Vier-Stunden-Kerzen sind der Takt des Diagramms, und veröffentlicht werden nur fertige, sodass jede Entscheidung auf einer bereits abgeschlossenen Kerze fällt.
- Ein Konverter liest aus jeder Kerze den Schlusskurs heraus, und ein auf derselben Reihe berechneter exponentieller Durchschnitt ist die Marke, an der der Kurs gemessen wird.
- Zwei Kreuzungsblöcke beobachten dieses Paar von entgegengesetzten Seiten: der eine nimmt den Schlusskurs als oberen und den Durchschnitt als unteren Eingang, beim anderen ist es umgekehrt. Jeder meldet sich nur auf der Kerze, auf der die beiden Linien tatsächlich die Plätze tauschen.
- Die aktuelle Position wird von einer Variablen auf den Kerzentakt gebracht: sie hält sie an ihrem Eingang und gibt sie beim Kerzen-Trigger frei; drei Vergleiche gegen Null machen aus dieser gehaltenen Zahl die Zustände flat, long und short.
- Vier logische UND-Gatter verknüpfen die beiden Kreuzungen mit diesen drei Zuständen: eine Kreuzung nach oben bei flacher Position eröffnet einen Long, eine Kreuzung nach unten bei flacher Position eröffnet einen Short, und jede der beiden Kreuzungen gegen eine bestehende Position stellt sie glatt.
- Beide Einstiegsblöcke führen die Bedingung zur offenen Position mit, sodass ein Gatter, das bei bereits laufendem Trade auslöst, weder aufstocken noch drehen kann: das Diagramm hält immer nur eine Position gleichzeitig.
- Jede eigene Ausführung erreicht Position protection über eine Combination, und genau das hält seine Vorstellung von der Position ehrlich — Einstiege schalten ihn scharf, glattstellende Ausführungen entschärfen ihn.
- An seinen Price-Eingang geht derselbe Schlusskurs, den auch die Signale verwenden, sodass die Trailing-Marke einmal je geschlossener Kerze neu berechnet wird; das Chart-Panel zeichnet die Kerzen, den Durchschnitt, die Einstiegs- und Ausstiegsorders, den schützenden Stop und jede Ausführung.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Auf einer geschlossenen Kerze kreuzt der Schlusskurs den exponentiellen Durchschnitt nach oben, während die gehaltene Position null ist. Das Long-Gatter lässt durch, und Position modify kauft das Ordervolumen zum Marktpreis.
- **Short-Einstieg**: Auf einer geschlossenen Kerze kreuzt der Schlusskurs den exponentiellen Durchschnitt nach unten, während die gehaltene Position null ist. Das Short-Gatter lässt durch, und Position modify verkauft das Ordervolumen zum Marktpreis.
- **Ausstieg**: Zwei Dinge können einen Trade beenden, und meist ist es das erste. Position protection legt seinen Stop 1.5% vom Einstiegskurs entfernt und trägt ihn bei eingeschaltetem Trailing hinter einem Long nach oben und hinter einem Short nach unten mit, jedes Mal wenn eine Kerze weiter im Gewinn schließt; der Trade wird per Market-Order geschlossen, sobald ein Schlusskurs wieder durch diese Marke zurückkommt. Der zweite Ausgang ist das Signal selbst: eine Kreuzung gegen eine offene Position löst einen dritten, auf Schließen gestellten Position-modify-Block aus, der glattstellt, was da ist, und dafür kein eigenes Volumen braucht. Keiner der beiden Wege dreht eine Position — die Gegenrichtung muss auf die nächste Kreuzung warten, und bis dahin ist das Diagramm flat und frei, sie zu nehmen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 04:00:00 | Zeitrahmen der Kerzenreihe. Eine geschlossene Kerze ist eine Entscheidung und eine Neuberechnung der Trailing-Marke. |
| EMA Length | 15 | Anzahl der Kerzen im exponentiellen Durchschnitt, an dem der Schlusskurs gemessen wird. Ein längerer kreuzt seltener und hält einen Trade durch mehr Rauschen hindurch. |
| Order Volume | 1 | Ordergröße in Lots. |
| Trailing Stop, % | 1.5 | Abstand des Trailing-Stops in Prozent. Er wird vom besten seit Eröffnung der Position erreichten Kurs gemessen, nicht vom Einstiegskurs. |

## Diagrammdetails

- Die Trailing-Marke folgt dem Preis, den der Block bekommt, und dieser Preis ist hier ein Schlusskurs. Der Stop liegt damit dort, wo die Kerzen geschlossen haben, und nicht dort, wohin ihre Dochte reichten, sodass eine Spitze innerhalb der Kerze die Marke weder mitzieht noch den Trade herauswirft.
- Ein feineres Nachziehen ist nur eine Verbindung entfernt: Speise den Price-Eingang aus einem Level 1-Block mit dem letzten Handelspreis, oder gib stattdessen das Orderbuch an den Market depth-Eingang, und derselbe Stop wird bei jeder Quotierung neu bepreist statt einmal alle vier Stunden.
- Jede eigene Ausführung geht in Position protection, auch die glattstellenden. Eine schließende Ausführung bringt seine laufende Position zurück auf null und entschärft den Stop; ohne sie würde der Block weiter eine Position bewachen, die es gar nicht mehr gibt, und sie am Ende schützen, indem er die entgegengesetzte eröffnet.
- Der Durchschnitt veröffentlicht nur ausgebildete und endgültige Werte, sodass die ersten Kerzen, in denen er sich noch füllt, keine eigene Kreuzung erzeugen können.
- Order Volume ist eine einzelne nach außen gelegte Zahl, die sich beide Einstiegsblöcke teilen. Der schließende Block nimmt überhaupt kein Volumen, denn eine Order zum Schließen der Position bemisst sich aus dem, was offen ist.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
