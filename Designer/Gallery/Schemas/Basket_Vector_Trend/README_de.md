# Diagramm der Strategie für gewichteten Korb-Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm handelt zwei Instrumente als einen Korb und entscheidet dabei anhand von drei Kursreihen statt zweier. Aus beiden gehandelten Instrumenten wird ein synthetisches Instrument zusammengesetzt; ein Divisor in dessen Ausdruck skaliert das teure Leg so weit herunter, dass das günstige das Ergebnis noch bewegen kann. Der Trend dieses synthetischen Instruments ist das, was das Diagramm die Richtung des Korbs nennt. Jedes Leg wird anschließend auf dieselbe Weise für sich gemessen. Ein Leg wird nur gekauft oder verkauft, wenn sein eigener Trend mit dem des Korbs übereinstimmt, und wenn der Korb dreht, wird jedes Leg geschlossen, das nun in die falsche Richtung zeigt. Sonst schließt nichts eine Position: Es gibt kein Ziel und keinen Stop, und das Vorzeichen des Korbs ist zugleich der Grund, im Markt zu sein, und der Grund, ihn zu verlassen.

![schema](schema.svg)

## Strategieübersicht

- Ein Security index-Block baut aus den beiden gehandelten Instrumenten ein synthetisches Instrument. Die Gewichtung steckt in dessen Ausdruck, sodass beide Legs auf vergleichbarer Skala beitragen und der größere Kurs den kleineren nicht übertönt.
- Drei Kerzenreihen laufen im selben Zeitrahmen: eine auf dem synthetischen Korb und je eine auf jedem Leg. Alle drei werden ausschließlich als abgeschlossene Kerzen abonniert, sodass jeder nachgelagerte Wert zu einer bereits geschlossenen Kerze gehört.
- Jede Reihe speist einen schnellen und einen langsamen geglätteten gleitenden Durchschnitt, und eine Formel zieht den langsamen vom schnellen ab. Das Ergebnis ist eine vorzeichenbehaftete Trenddifferenz, und davon gibt es drei: eine für den Korb und je eine für jedes Leg.
- Jede Differenz wird von einer Variablen aufgefangen, die sie hält und freigibt, sobald eine Kerze des First Leg abgeschlossen ist. So entsteht eine Entscheidung nie aus einem Korbwert von einem Zeitpunkt und einem Leg-Wert von einem anderen.
- Der Wert stammt aus der Reihe, die ihn zuletzt geliefert hat, der Zeitpunkt stammt aus der gehandelten Kerze, und alles Nachgelagerte trägt deshalb den Zeitstempel der Kerze, auf der die Order gesendet wird.
- Sechs Vergleiche verwandeln die drei zwischengespeicherten Differenzen in vorzeichenbehaftete Flags gegen null: Korb aufwärts oder abwärts, First Leg aufwärts oder abwärts, Second Leg aufwärts oder abwärts.
- Zwei Positionsblöcke, je einer an ein Instrument gebunden, melden den bereits gehaltenen Bestand, und zwei weitere Vergleiche sagen, ob dieses Leg gerade ohne Position ist. Genau das verhindert, dass ein wiederholtes Signal einen zweiten Einstieg in dasselbe Leg aufsattelt.
- Vier logische Bedingungen fassen je drei Flags zusammen — Korbrichtung, Leg-Richtung, Leg ohne Position — und jede von ihnen löst einen Block für eine Market-Order aus. Vier weitere Order-Blöcke übernehmen die Ausstiege, direkt gesteuert von den beiden Korb-Flags.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Ein Leg wird gekauft, wenn die Korbdifferenz positiv ist, die eigene Differenz dieses Legs positiv ist und in diesem Leg nichts gehalten wird. Über beide Legs wird auf derselben Kerze unabhängig voneinander entschieden: Beide können gleichzeitig long sein, eines kann long sein, während das andere draußen bleibt, oder keines erfüllt die Bedingungen.
- **Short-Einstieg**: Ein Leg wird verkauft, wenn die Korbdifferenz negativ ist, die eigene Differenz dieses Legs negativ ist und in diesem Leg nichts gehalten wird. Die Short-Seite ist das exakte Spiegelbild der Long-Seite, und dieselbe Unabhängigkeit zwischen den Legs gilt auch hier.
- **Ausstieg**: Der einzige Ausstieg ist ein Vorzeichenwechsel der Korbdifferenz. Ein negativer Korb löst die beiden Schließen-Blöcke mit Verkaufsrichtung aus, ein positiver Korb die beiden mit Kaufrichtung. Die Richtung an einem Schließen-Block ist ein Filter und keine Anweisung: Der Block wirkt nur auf eine Position, die in die entgegengesetzte Richtung zeigt, sodass ein Schließen-Block auf der Verkaufsseite ein Long-Leg anfasst und überhaupt nichts tut, wenn dieses Leg ohne Position oder bereits short ist. Ein Leg, dessen eigene Differenz gedreht hat, dessen Korb aber nicht, bleibt unangetastet, bis der Korb zustimmt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| First Leg | BTCUSDT@BNBFT | Das erste gehandelte Instrument. Es speist seine eigene Kerzenreihe, sein eigenes Paar gleitender Durchschnitte, seinen eigenen Positionsblock und seine eigenen vier Order-Blöcke; es zu ändern verschiebt dieses gesamte Leg. |
| Second Leg | TONUSDT@BNBFT | Das zweite gehandelte Instrument, genauso verdrahtet wie das erste. Beide Legs sind symmetrisch, und keines ist dem anderen untergeordnet. |
| Basket Index | BTCUSDT@BNBFT / 20000 + TONUSDT@BNBFT | Der Ausdruck, aus dem das synthetische Korbinstrument aufgebaut wird. Der Divisor bringt beide Legs auf eine vergleichbare Skala — größer gewählt lässt er das Second Leg dominieren, kleiner gewählt gibt er dem First Leg mehr Gewicht — und das Vorzeichen dieser Reihe gibt jeden Einstieg frei. |
| Basket Candles | 00:15:00 | Zeitrahmen der Korbkerzen. Halten Sie ihn gleich den Zeitrahmen der Legs: Die drei Ströme sollen als eine Kerze gelesen werden. |
| First Leg Candles | 00:15:00 | Zeitrahmen der Kerzen des First Leg. Diese Reihe ist zugleich die Handelsuhr: Sie löst die Zwischenspeicher, die Konstanten und damit den Zeitpunkt jeder gesendeten Order aus. |
| Second Leg Candles | 00:15:00 | Zeitrahmen der Kerzen des Second Leg. Er wird gleich dem des First Leg gehalten, damit beide Legs auf Kerzen gleicher Länge beurteilt werden. |
| Basket Fast Length | 3 | Länge des schnellen Durchschnitts auf dem Korb. Kürzer reagiert früher und dreht das Vorzeichen des Korbs häufiger, was Positionen sowohl öfter eröffnet als auch öfter schließt. |
| Basket Slow Length | 7 | Länge des langsamen Durchschnitts auf dem Korb. Der Abstand zwischen dieser und der schnellen Länge legt fest, wie deutlich eine Bewegung sein muss, bevor der Korb als gedreht gilt. |
| First Leg Fast Length | 3 | Länge des schnellen Durchschnitts auf dem First Leg. Sie entscheidet nur, ob dieses Leg mit dem Korb übereinstimmt; die Richtung des Korbs legt sie nie selbst fest. |
| First Leg Slow Length | 7 | Länge des langsamen Durchschnitts auf dem First Leg. Ein größerer Abstand zwischen beiden Längen lässt dieses Leg seltener bestätigen, sodass der Korb drehen kann, ohne dass es mitgeht. |
| Second Leg Fast Length | 3 | Länge des schnellen Durchschnitts auf dem Second Leg; sie spielt für dieses Instrument dieselbe bestätigende Rolle. |
| Second Leg Slow Length | 7 | Länge des langsamen Durchschnitts auf dem Second Leg. Beide Legs dürfen bewusst unterschiedlich eingestellt werden, wenn eines von beiden das unruhigere der beiden ist. |
| First Leg Volume | 0.1 | Größe einer Order im First Leg, in den eigenen Einheiten dieses Instruments. Es lohnt sich, diesen Wert gemeinsam mit dem Volumen des Second Leg zu setzen, damit ein vollständiger Korb auf beiden Seiten vergleichbares Kapital einsetzt. |
| Second Leg Volume | 2000 | Größe einer Order im Second Leg. Beide Volumina sind getrennt, weil die Instrumente auf völlig unterschiedlichen Skalen notieren und eine einzige gemeinsame Zahl ein Leg bedeutungslos machen würde. |

## Diagrammdetails

- Die Gewichtung, die beide Legs ausbalanciert, ist Teil des Index-Ausdrucks und keine im Diagramm verdrahtete Zahl. Den Korb neu zu gewichten heißt, diese eine Parameterzeichenfolge zu bearbeiten, und die gesamte synthetische Reihe wird daraus neu aufgebaut.
- Kerzen werden ausschließlich als abgeschlossen abonniert. Eine Order, die aus einer noch laufenden Kerze heraus abgesetzt wird, trägt den Zeitstempel der Kerzeneröffnung, also einen früheren Zeitpunkt als den ihrer tatsächlichen Absendung, und wird genau deshalb abgelehnt; nur geschlossene Kerzen zu verwenden hält jede Order mit dem Zeitpunkt gestempelt, zu dem sie gehört.
- Erst die drei zwischenspeichernden Variablen machen den Korb überhaupt nutzbar. Ein synthetisches Instrument wird aus zwei Datenströmen zusammengesetzt, und seine Kerze wird später fertig als eine gewöhnliche, sodass ein Block, der darauf wartet, dass alle drei Werte in dasselbe Zeitfenster fallen, auf einen Satz von Werten wartet, der nie eintrifft. Jeden Wert an die gehandelte Kerze zu koppeln nimmt den Wert so, wie er gerade steht, und den Zeitpunkt aus der Kerze, und die Vergleiche, die logischen Bedingungen und die Orders laufen damit alle nach der Uhr der gehandelten Kerze.
- Die Einstiegsblöcke sind auf das Eröffnen einer Position gesetzt, ein Einstieg wirkt also nur aus dem Zustand ohne Position heraus. Ein Signal, das über mehrere Kerzen wahr bleibt, erzeugt daher eine Order und nicht eine pro Kerze, und nur die Ausstiegsblöcke führen zurück in den Zustand ohne Position.
- Das Chart-Panel zeichnet alle drei Kerzenreihen, alle sechs gleitenden Durchschnitte sowie jede Order und jede Ausführung aus allen acht Order-Blöcken, sodass sich eine Kerze zusammen mit dem Korb lesen lässt, der sie freigegeben hat, und dem Leg, das sie bestätigt hat.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
