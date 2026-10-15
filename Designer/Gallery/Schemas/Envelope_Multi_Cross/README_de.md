# Diagramm der Strategie Envelope Multi Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein schneller und ein langsamer Durchschnitt treffen sich nicht in einem einzigen sauberen Punkt: Sie berühren sich, laufen auseinander und berühren sich erneut rund um dieselbe Zone. Dieses Diagramm nimmt das hin und macht aus der Zone drei Ebenen — ein schmales Band ober- und unterhalb des langsamen Durchschnitts sowie den langsamen Durchschnitt selbst. Jede Ebene erhält einen eigenen Kreuzungsbaustein, und zwei Combination-Bausteine bündeln sechs unabhängige Kreuzungen zu einem Long-Strom und einem Short-Strom, sodass eine ganze Leiter von Signalen bei einem einzigen Paar von Order-Bausteinen ankommt.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Kerzen eines Instruments speisen zwei exponentielle gleitende Durchschnitte — einen schnellen und einen langsamen — sowie einen Konverter, der den Schlusskurs aus derselben Kerze ausliest.
- Zwei Formeln nehmen den langsamen Durchschnitt und skalieren ihn um einen festen Anteil nach oben und nach unten; daraus entstehen ein oberes und ein unteres Envelope. Mit dem langsamen Durchschnitt dazwischen hat das Diagramm drei Ebenen statt einer.
- Drei Kreuzungsbausteine beobachten, wie der schnelle Durchschnitt durch das untere Envelope, durch den langsamen Durchschnitt und durch das obere Envelope nach oben steigt — jeder an seinem eigenen Eingangspaar.
- Drei weitere Kreuzungsbausteine führen dieselben drei Ebenen mit vertauschten Eingängen — Ebene oben, schneller Durchschnitt unten —, sodass sie melden, wenn der schnelle Durchschnitt durch diese Ebenen nach unten sinkt.
- Ein Combination-Baustein fasst die drei Aufwärtskreuzungen zu einem einzigen Strom zusammen, ein zweiter die drei Abwärtskreuzungen; ab dieser Stelle ist die ganze Leiter nur noch ein Signal je Seite.
- Jeder Strom wird durch einen Vergleich des Schlusskurses mit dem schnellen Durchschnitt bestätigt, sodass ein Durchstoßen nur dann als Einstieg zählt, solange der Kurs auf derselben Seite der schnellen Linie liegt.
- Einstiege sind Market-Orders mit festem Volumen aus einer flachen Position heraus; der Gegenstrom steuert unbearbeitet und für sich allein einen Baustein zum Schließen der Position.
- Position protection übernimmt jede Einstiegsausführung und begleitet sie mit einem prozentualen Take-Profit und einem nachgezogenen Stop-Loss.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Long-Strom feuert: Der schnelle Durchschnitt hat das untere Envelope, den langsamen Durchschnitt oder das obere Envelope nach oben gekreuzt. Der Vergleich bestätigt, dass die Kerze über dem schnellen Durchschnitt geschlossen hat, beide Antworten treffen in einer logischen Bedingung zusammen, und der Baustein Position modify kauft das Ordervolumen zum Markt. Die Einstellung zum Eröffnen der Position lässt diese Order nur durch, solange die Position flach ist.
- **Short-Einstieg**: Der Short-Strom feuert auf dieselbe Weise, auf den gespiegelten Kreuzungen: Der schnelle Durchschnitt ist durch das obere Envelope, durch den langsamen Durchschnitt oder durch das untere Envelope gesunken. Der Vergleich bestätigt, dass die Kerze unter dem schnellen Durchschnitt geschlossen hat, und der Baustein Position modify verkauft das Ordervolumen aus einer flachen Position heraus zum Markt.
- **Ausstieg**: Zwei voneinander unabhängige Dinge beenden einen Trade. Der Gegenstrom, ohne die Kursbestätigung genommen, löst einen Baustein zum Schließen der Position aus: Jedes Durchstoßen nach unten stellt eine Long-Position glatt, jedes Durchstoßen nach oben eine Short-Position. Parallel dazu beobachtet Position protection die Einstiegsausführungen, liest den Schlusskurs und schließt den Trade am Take-Profit oder am nachgezogenen Stop — je nachdem, was zuerst erreicht wird.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:15:00 | Zeitrahmen der Kerzenserie, auf der alles Übrige berechnet wird. |
| Fast EMA Length | 10 | Länge des schnellen exponentiellen gleitenden Durchschnitts — der Linie, die durchstößt. |
| Slow EMA Length | 30 | Länge des langsamen exponentiellen gleitenden Durchschnitts — der Linie, um die das Band gezogen wird. |
| Envelope Buffer | 0.003 | Halbe Breite des Bandes als Anteil des langsamen Durchschnitts: 0.003 legt die Envelopes 0.3% darüber und darunter. |
| Order Volume | 1 | Ordergröße in Lots für beide Einstiegsrichtungen. |
| Take Profit, % | 1.5 | Take-Profit-Abstand in Prozent des Einstiegskurses. |
| Stop Loss, % | 0.8 | Stop-Loss-Abstand in Prozent des Einstiegskurses; er wird einer Position nachgezogen, die sich in die gewünschte Richtung bewegt. |

## Diagrammdetails

- Erst die beiden Combination-Bausteine machen die Leiter lesbar. Ohne sie bräuchte jede der sechs Kreuzungen eine eigene Verbindung zu den Order-Bausteinen, und eine vierte Ebene zu ergänzen hieße, die gesamte rechte Seite des Diagramms neu zu zeichnen.
- Ein Kreuzungsbaustein meldet die Richtung, in der gekreuzt wurde, und eine Abwärtskreuzung kommt als negatives Signal an, das ein Order-Auslöser stillschweigend ignoriert. Deshalb ist der Abwärtssatz aus drei eigenen Bausteinen mit vertauschten Eingängen aufgebaut und nicht durch Negation des Aufwärtssatzes.
- Kerzen werden ausschließlich als abgeschlossen abonniert. Ein Update einer noch entstehenden Kerze trägt den Zeitstempel der Bar-Eröffnung, und eine Order mit einem Zeitstempel vor dem aktuellen Moment wird abgelehnt.
- Einstiege nutzen die Bedingung zum Eröffnen der Position; ein Signal, das bei bereits laufendem Trade eintrifft, kostet daher nichts: Der Baustein meldet ein ungültiges Volumen, und es wird keine Order geschickt. Diese eine Einstellung leistet das, wofür sonst ein expliziter Positionsfilter in der Einstiegsbedingung nötig wäre.
- Das Band ist bewusst schmal. Es ist ein Puffer um den langsamen Durchschnitt und kein Volatilitätskanal, sodass die beiden zusätzlichen Ebenen nahe an der einfachen Kreuzung auslösen und das Signal verdichten, statt es zu ersetzen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
