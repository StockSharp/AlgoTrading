# Strategiediagramm Candle Streak Profit Lock
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Vier Kerzen, die alle in dieselbe Richtung schließen, bilden eine Serie, und dieses Diagramm handelt in Richtung dieser Serie. Der Einstieg ist die einfache Hälfte; interessant ist der Ausstieg, denn dafür wartet das Diagramm nicht auf ein Kursniveau. Es beobachtet den Geldbetrag in der offenen Position, und sobald dieser Wert zum ersten Mal einen festgelegten Betrag erreicht, wird die Position geschlossen und der Gewinn realisiert. Eine Verriegelung sorgt dafür, dass dieser Befehl einmal gesendet wird und nicht bei jeder Aktualisierung des Ergebnisses.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Vier-Stunden-Kerzen werden einmal gelesen und mehrfach verwendet: Die aktuelle Kerze geht direkt an je einen Konverter für Close und Open, und drei Previous value-Blöcke liefern die Kerzen ein, zwei und drei Bars zurück, jede mit einem eigenen Close- und Open-Konverter.
- Acht Vergleiche machen aus diesen vier Paaren die Farbe jedes Bars: Close über Open ist eine steigende Kerze, Close unter Open eine fallende, und ein Bar, der genau dort schließt, wo er eröffnet hat, ist keines von beidem — er besteht beide Prüfungen nicht und kann keine der beiden Serien verlängern.
- Ein logisches UND sammelt die vier Antworten für steigende Kerzen, ein zweites die vier für fallende; jedes liefert nur dann true, wenn alle vier Bars im Fenster übereinstimmen — genau das ist eine Serie.
- Der mit null verglichene Positionsblock sagt, ob das Diagramm flat, long oder short ist, und jedes nachgelagerte Gatter ist ein logisches UND aus einer Serie und einem Positionszustand.
- Beide Einstiege sind Market-Orders mit festem Volumen und tragen die Bedingung zur offenen Position, sodass eine Serie, die weiterläuft, während bereits eine Position offen ist, keine zweite Order auf die erste stapeln kann.
- Die Ausführungen der Einstiege werden zu einer Leitung zusammengeführt und an Position protection übergeben; dieser Block führt einen Take-Profit und einen Trailing-Stop als Prozentsatz des Ausführungspreises und stellt sie, solange die Position besteht.
- Der Strategy P&L-Block liefert das offene Ergebnis der laufenden Position in eigenem Takt, und ein Vergleich hält diesen Wert gegen Profit Lock; die Antwort geht in einen Flag-Block, der das erste true durchlässt und danach gesetzt bleibt, sodass genau ein Schließen-Befehl gesendet wird.
- Eine Serie, die sich gegen eine offene Position bildet, schließt sie zu Markt, und der Flag-Block wird von jedem der beiden Einstiegsgatter neu scharf gestellt, sodass jede neue Position wieder mit einsatzbereiter Verriegelung startet.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Vier abgeschlossene Kerzen in Folge schließen über ihrem eigenen Open, während die Position flat ist: Position modify kauft das Ordervolumen zu Markt.
- **Short-Einstieg**: Vier abgeschlossene Kerzen in Folge schließen unter ihrem eigenen Open, während die Position flat ist: Position modify verkauft das Ordervolumen zu Markt.
- **Ausstieg**: Drei Auswege, und die Position verlässt den Markt über den, der zuerst eintritt. Profit Lock schließt sie, sobald das offene Ergebnis den festgelegten Betrag erreicht; da der Flag-Block dieses Signal bereits einmal durchgelassen hat, senden spätere Aktualisierungen desselben Ergebnisses nichts mehr. Position protection schließt sie über den Take-Profit oder den Trailing-Stop, wobei der Stop dem Kurs folgt, sobald der Trade im Gewinn liegt. Eine Serie in die Gegenrichtung schließt sie ebenfalls zu Markt, und diese Schließen-Order ist alles, was auf diesem Bar geschieht: Die Einstiegsgatter verlangen eine flache Position, sodass eine Umkehr von dem nächsten Serien-Signal eröffnet wird, das das Diagramm leer vorfindet, und nicht von demselben, das es geleert hat.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Type | 04:00:00 | Zeitrahmen der Kerzenreihe, auf der die Serie gezählt wird; die vier Bars einer Serie sind vier Kerzen dieser Länge. |
| Order Volume | 1 | Ordergröße in Lots, die jeder Einstieg sendet; sie skaliert zugleich das offene Ergebnis, mit dem Profit Lock verglichen wird. |
| Take Profit | 1% | Take-Profit-Abstand vom Ausführungspreis, in Prozent. |
| Stop Loss | 1% | Stop-Loss-Abstand vom Ausführungspreis, in Prozent; der Stop zieht nach, folgt also dem Kurs, sobald sich der Trade zugunsten der Position bewegt, und geht nie zurück. |
| Profit Lock | 200 | Offenes Ergebnis in der Portfoliowährung, bei dem die Position geschlossen und der Gewinn realisiert wird. |

## Diagrammdetails

- Die Länge der Serie ist im Diagramm verdrahtet und nicht als Zahl eingestellt: vier Bars bedeuten drei Previous value-Blöcke und vier Eingänge an jedem Serien-Gatter. Eine Serie von fünf bedeutet drei Blöcke mehr und einen Eingang mehr pro Gatter; eine Serie von drei einen Block weniger.
- Jeder Previous value-Block ist auf den Typ Kerze gesetzt und liest die Kerzenreihe direkt; Close und Open werden aus dem entnommen, was er zurückgibt. Zuerst einen Preis zu nehmen und dann nach dessen vorherigem Wert zu fragen, ist die umgekehrte Reihenfolge — und genau die lässt den Vergleich ohne Operand.
- Profit Lock ist ein Betrag in der Portfoliowährung und keine Kursdistanz, hängt also ebenso vom Ordervolumen ab wie vom Instrument. Eine Verdopplung des Volumens halbiert die Bewegung, die zum Erreichen nötig ist; der Wechsel auf ein Instrument mit anderer Preisskala verändert ihn vollständig.
- Profit Lock und Take-Profit sind zwei Antworten auf dieselbe Frage, und die engere gewinnt. Setzt man den Lock so, dass er vor dem Take-Profit auslöst, wird der Take-Profit zur Obergrenze, die nur erreicht wird, wenn ein Bar über den Lock hinausspringt; setzt man ihn weit, ist der Take-Profit der gewöhnliche Ausstieg und der Lock das Sicherheitsnetz dahinter.
- Alles, was das Diagramm sendet, ist eine Market-Order, und die Kerzenreihe wird ausschließlich als abgeschlossene Bars abonniert. Ein Signal aus einem noch nicht abgeschlossenen Bar trägt dessen Eröffnungszeit, und eine Order mit einem bereits in der Vergangenheit liegenden Zeitstempel wird abgelehnt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
