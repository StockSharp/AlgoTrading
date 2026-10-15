# Diagramm der Strategie Global Stop Timer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Über die Einstiege entscheidet der Kurs, über die Ausstiege entscheidet das Geld. Kreuzt das Momentum sein neutrales Niveau, wird eine Position in die Richtung eröffnet, in die der gleitende Durchschnitt ohnehin schon zeigt, und von diesem Moment an gehört der Trade dem offenen Ergebnis der Strategie: Der Strategy-P&L-Block beobachtet jede Änderung des unrealisierten Werts und stellt die Position glatt, sobald dieser auf den Money Stop fällt oder das Money Target erreicht — ganz gleich, was die Indikatoren gerade sagen.

![schema](schema.svg)

## Strategieübersicht

- Eine einzige Kerzenserie, fünf Minuten und ausschließlich abgeschlossene Bars, versorgt alles: beide Indikatoren, den Konverter des Schlusskurses und die Trigger der konstanten Variablen.
- Das Momentum misst, wie weit der Kurs über seine eigene Länge gelaufen ist; der Crossing-Block vergleicht es mit einem in einer Variablen gehaltenen Niveau und feuert nur in dem Moment, in dem beide die Seiten tauschen — true bei einer Kreuzung nach oben, false bei einer nach unten.
- Ein logisches NOT macht aus derselben Kreuzung das Abwärtsereignis, sodass ein einziger Crossing-Block beide Richtungen bedient und beide niemals auf derselben Bar feuern können.
- Ein Konverter holt den Schlusskurs aus der Kerze, und zwei Vergleiche stellen ihn über oder unter den gleitenden Durchschnitt — das ist der Trendfilter, den beide Einstiege passieren müssen.
- Der Position-Block wird dreimal mit null verglichen: flach sichert die Einstiege ab, long und short schärfen die beiden Signalausstiege.
- Jedes Einstiegs-Gate ist ein logisches AND aus drei Antworten — der Kreuzung, der Seite des gleitenden Durchschnitts und einer flachen Position —, und der Einstieg selbst ist eine Market-Order, die nur aus der flachen Position heraus erfolgt; ein Signal, das während eines laufenden Trades eintrifft, ändert somit nichts.
- Der Strategy-P&L-Block hat weder Eingänge noch einen eigenen Kerzentakt: Er gibt das unrealisierte Ergebnis bei jeder Änderung aus, und zwei Vergleiche messen diese Zahl gegen den Money Stop und das Money Target, die in Variablen gehalten werden.
- Das Geld-Urteil und die beiden Urteile der Gegenkreuzung laufen in einem Combination-Block zusammen, der ein einziges auf Schließen gesetztes Position modify ansteuert: Welcher Grund auch zuerst eintritt — die Position wird durch dieselbe Market-Order glattgestellt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Das Momentum kreuzt sein Niveau nach oben, die Kerze schließt über dem gleitenden Durchschnitt und die Position ist flach. Position modify kauft das Order Volume zum Markt.
- **Short-Einstieg**: Das Momentum kreuzt sein Niveau nach unten — derselbe Crossing-Block, gelesen über das logische NOT —, die Kerze schließt unter dem gleitenden Durchschnitt und die Position ist flach. Position modify verkauft das Order Volume zum Markt.
- **Ausstieg**: Drei Gründe schließen einen Trade, und alle drei enden am selben Block. Das unrealisierte Ergebnis der Strategie fällt auf den Money Stop; oder es erreicht das Money Target; oder das Momentum kreuzt sein Niveau zurück, während die Position in die entgegengesetzte Richtung offen ist. Die ersten beiden werden bei jeder Änderung des P&L geprüft und nicht im Kerzentakt, sodass auf eine schnelle Bewegung noch innerhalb der Bar reagiert wird; der dritte wird einmal je abgeschlossener Kerze geprüft. Die schließende Order wird aus der aktuellen Position dimensioniert, lässt die Strategie also immer flach zurück und dreht die Position nie in einem Schritt um.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzenserie, auf der das gesamte Diagramm läuft; verwendet werden nur abgeschlossene Kerzen. |
| Momentum Length | 10 | Länge des Momentum-Indikators — wie weit zurück die Kursbewegung gemessen wird. |
| EMA Length | 20 | Länge des gleitenden Durchschnitts, der entscheidet, auf welcher Seite des Trends ein Einstieg erlaubt ist. |
| Momentum Level | 0 | Niveau, mit dem das Momentum verglichen wird. Null ist sein neutraler Wert: darüber liegt der Kurs höher als eine Länge zuvor, darunter tiefer. |
| Order Volume | 1 | Ordergröße in Lots für beide Einstiege. Die schließende Order ignoriert sie und wird aus der Position dimensioniert. |
| Money Stop | -250 | Verlust der offenen Position in der Währung des Kontos, bei dem die Position geschlossen wird. Negativ. |
| Money Target | 500 | Gewinn der offenen Position in der Währung des Kontos, bei dem die Position geschlossen wird. |

## Diagrammdetails

- Beide Indikatoren sind so eingestellt, dass sie ausschließlich ausgeformte und endgültige Werte ausgeben, sodass eine unfertige Kerze keine Kreuzung erzeugen kann.
- Das Niveau, gegen das das Momentum gemessen wird, liegt in einer Variablen und nicht im Vergleich selbst — genau das macht es zu einem Schemaparameter, der sich ändern und optimieren lässt, ohne das Diagramm zu öffnen.
- Jede konstante Variable wird getriggert — die drei kerzengetriebenen von der Kerzenserie, die beiden Geldschwellen vom unrealisierten P&L —, denn eine Variable hält ihren Wert, gibt ihn aber nur aus, wenn etwas danach fragt.
- Die Einstiege tragen die Bedingung zur offenen Position, sie feuern also nur aus der flachen Position heraus. Ohne sie würde bei jeder Änderung der Position eine Market-Order gesendet und der Lauf würde sich mit unerwünschten Trades füllen.
- Money Stop und Money Target sind Beträge in der Währung des Kontos und keine Abstände im Kurs; sie müssen daher gemeinsam mit dem Order Volume neu skaliert werden, wenn das Diagramm auf ein anderes Instrument übertragen wird.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
