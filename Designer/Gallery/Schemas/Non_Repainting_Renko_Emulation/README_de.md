# Strategiediagramm Non Repainting Renko Emulation
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Händler greifen zu Brick-Charts, weil ein Brick, einmal gezeichnet, sich nie mehr ändert: Ein darauf gefasstes Signal lässt sich eine Minute später nicht zurücknehmen. Dieses Diagramm holt dieselbe Eigenschaft aus gewöhnlichen Zeitkerzen heraus. Die Kerzenquelle wird bewusst mit Zwischenaktualisierungen abonniert, und zwischen ihr und jedem denkenden Block steht ein Final value, sodass ein Durchschnitt, eine Kreuzung und ein Einstieg allesamt auf einer bereits geschlossenen Kerze entschieden werden. Der Live-Strom bleibt erhalten, geht aber nur an das Chart, wo eine sich bildende Kerze hingehört.

![schema](schema.svg)

## Strategieübersicht

- Fünf-Minuten-Kerzen werden mit aktivierten Zwischenaktualisierungen abonniert, sodass die Quelle die Kerze vielfach ausgibt, während sie sich bildet, und ein weiteres Mal, wenn sie schließt.
- Ein Final value vom Typ Kerze ist die einzige Tür von der Quelle in die Logik: Er gibt einen Wert erst weiter, wenn dieser endgültig ist, sodass nachgelagert nie ein Preis zu sehen ist, der sich noch bewegen kann.
- Dieser Block ist tragend und nicht bloß Zierde. Ein Indikator-Block markiert jeden ihm übergebenen Wert als endgültig; ein Durchschnitt, der mit einer sich bildenden Kerze gefüttert wird, würde den Wert derselben Kerze bei jeder Aktualisierung überschreiben, und eine Kreuzung zweier solcher Durchschnitte würde innerhalb der Kerze auftauchen und wieder verschwinden.
- Ein schneller und ein langsamer exponentieller Durchschnitt sowie ein Relative-Stärke-Index werden alle auf dem geschlossenen Strom berechnet, und alle drei melden sich erst, wenn sie ausgeformt sind, sodass die ersten Entscheidungen auf ein vollständiges langsames Fenster warten.
- Zwei Crossing-Blöcke tragen die beiden Seiten desselben Ereignisses: Bei einem liegt der schnelle Durchschnitt am Up-Eingang und der langsame am Down-Eingang, beim anderen sind sie vertauscht, sodass jeder bei genau der Kreuzung true liefert, nach der er benannt ist.
- Der Relative-Stärke-Index wird in beide Richtungen mit einer Mittellinien-Variablen verglichen; daraus entsteht ein Momentum-Filter, der mit der Kreuzung übereinstimmen muss, bevor irgendetwas gesendet wird.
- Die Position wird in eine haltende Variable eingelesen, die von jeder geschlossenen Kerze angestoßen wird, und zweimal mit null verglichen, sodass der Einstieg eine nicht long stehende Position verlangen kann und der Ausstieg eine long stehende.
- Zwei logische UND-Blöcke sammeln Kreuzung, Momentum und Position ein; einer treibt einen Markteinstieg mit der Bedingung Open position an, der andere einen Marktausstieg mit der Bedingung Close position, und das Chart-Panel zeichnet die Live-Kerzen, alle drei Indikatoren, die Orders und die Ausführungen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Auf einer geschlossenen Kerze kreuzt der schnelle Durchschnitt über den langsamen, der Relative-Stärke-Index steht über seiner Mittellinie und die Position ist nicht long. Der UND-Block sammelt die drei Antworten ein, und der Einstiegsblock kauft das Ordervolumen zum Marktpreis. Seine Bedingung Open position bedeutet, dass die Order nur aus einer flachen Position heraus gesendet wird; eine zweite Kreuzung während eines laufenden Trades ändert also nichts.
- **Short-Einstieg**: Short-Einstiege gibt es nicht. Unterhalb der Mittellinie oder wenn der schnelle Durchschnitt unter dem langsamen liegt, hält sich das Diagramm einfach heraus; die einzige Order, die es je in Verkaufsrichtung sendet, ist jene, die eine Long-Position schließt.
- **Ausstieg**: Auf einer geschlossenen Kerze kreuzt der schnelle Durchschnitt zurück unter den langsamen, der Relative-Stärke-Index ist unter seine Mittellinie gefallen und die Position ist long. Der zweite UND-Block löst aus, und der schließende Block verkauft die gesamte Position zum Marktpreis; sein Volumen berechnet die Bedingung Close position aus der offenen Position. Es gibt weder einen Stop-Loss- noch einen Take-Profit-Block: Dieselbe Kreuzung, die den Trade eröffnet hat, beendet ihn auch, und jede dieser Entscheidungen fällt auf einer Kerze, die bereits abgeschlossen ist.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzenserie, auf der das gesamte Diagramm läuft; die Serie wird mit Zwischenaktualisierungen abonniert, und der Final value sortiert die geschlossenen Kerzen daraus heraus. |
| Fast EMA Length | 14 | Länge des schnellen exponentiellen Durchschnitts, der schnelleren der beiden Linien, deren Kreuzungen das Signal bilden. |
| Slow EMA Length | 40 | Länge des langsamen exponentiellen Durchschnitts, der Referenzlinie, an der der schnelle gemessen wird. |
| RSI Length | 14 | Länge des Relative-Stärke-Index, der auf beiden Seiten als Momentum-Filter dient. |
| RSI Midline | 50 | Niveau, das den Momentum-Filter teilt: darüber darf das Diagramm eine Long-Position eröffnen, darunter darf eine Long-Position geschlossen werden. |
| Order Volume | 1 | Ordergröße, die der Einstieg sendet; der Ausstieg bezieht seine Größe stattdessen aus der offenen Position. |

## Diagrammdetails

- Das Gatter löst genau auf der Kreuzungskerze aus. Ein Crossing-Block gibt nur in dem Moment etwas aus, in dem die beiden Linien die Plätze tauschen, während die Vergleiche auf jeder geschlossenen Kerze ausgeben; eine logische Bedingung hält jeden Eingang fest, bis seit ihrem letzten Auslösen alle eingetroffen sind. Das fehlende Stück ist damit immer die Kreuzung, und die Antworten, mit denen sie kombiniert wird, stammen aus derselben Kerze.
- Die beiden Crossing-Blöcke sind derselbe Block mit vertauschten Eingängen. Jeder gibt true aus, wenn sein eigener Up-Eingang seinen Down-Eingang erreicht oder überschritten hat, und false beim umgekehrten Ereignis; deshalb ist einer nach der bullischen und der andere nach der bärischen Kreuzung benannt, statt dass ein Block beide Gatter speist.
- Der Positionsblock meldet sich nur, wenn sich die Position ändert, in einer ruhigen Woche also womöglich gar nicht. Die dahinterliegende haltende Variable startet bei null, behält die zuletzt erhaltene Zahl und gibt sie auf jeder geschlossenen Kerze erneut aus, sodass beide Vergleiche stets eine frische linke Seite haben, mit der sie arbeiten können.
- Nichts staffelt die Signale zeitlich. Jede qualifizierende Kreuzung wird genommen, und das Einzige, was verhindert, dass ein Einstieg auf dem anderen landet, ist die Forderung nach einer nicht long stehenden Position, abgesichert durch die Bedingung Open position am Orderblock selbst.
- Die sich bildende Kerze wird nicht verworfen, sie wird nur von den Entscheidungen ferngehalten: Der Rohstrom wird im Chart neben den Indikatoren gezeichnet, die aus dem geschlossenen Strom geplottet werden, sodass sich beide gegeneinander lesen lassen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
