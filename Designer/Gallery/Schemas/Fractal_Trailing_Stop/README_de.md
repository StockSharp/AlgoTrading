# Diagramm der Fractal-Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Fraktal ist eine Kerze, deren Hoch das höchste von fünf ist oder deren Tief das tiefste von fünf ist, und es lässt sich erst zwei Kerzen nach seinem Auftreten benennen. Das Diagramm bildet diese Verzögerung ehrlich ab: Es arbeitet ausschließlich mit abgeschlossenen Kerzen, macht aus jedem Fraktal einen Stop-Kurs und lässt diesen Kurs nur in eine Richtung wandern. Jeder Bruch eines Stops dreht die Position um, und das gebrochene Niveau tritt zurück, bis ein neues Fraktal es erneut scharf stellt.

![schema](schema.svg)

## Strategieübersicht

- Eine einzige Kerzenserie speist alles, und sie ist so eingestellt, dass sie nur abgeschlossene Kerzen liefert; damit entsteht kein Niveau und keine Order aus einem Kurs, den ein späterer Tick noch zurücknehmen könnte.
- Highest und Lowest nehmen das Extrem der letzten fünf Kerzen, während Previous value die Kerze von vor zwei Bars zurückgibt und zwei Konverter deren Hoch und deren Tief auslesen.
- Ist das Hoch von vor zwei Bars das höchste des Fensters, so ist diese Kerze ein oberes Fraktal; die spiegelbildliche Prüfung am Tief kennzeichnet ein unteres Fraktal. Eine haltende Variable speichert jeden Fraktalkurs und gibt ihn nur auf dem Bar heraus, auf dem die Prüfung bestanden wurde.
- Eine Formel addiert den Pufferprozentsatz zum oberen Fraktalkurs und zieht ihn vom unteren ab und macht so aus einem Fraktal einen Stop-Kurs.
- Zwei weitere Formeln sperren diese Kurse mit min und max wie eine Ratsche: Der obere Stop darf nur fallen und der untere Stop nur steigen, und genau das macht sie zu Trailing-Stops statt zu bloßen Swing-Niveaus.
- Der Schlusskurs jeder abgeschlossenen Kerze wird an beiden Stops gemessen: oberhalb des oberen Stops dreht das Diagramm auf Long, unterhalb des unteren Stops dreht es auf Short.
- Eine Umkehr besteht aus einem Blockpaar, von dem der eine das Offene schließt und der andere die neue Seite eröffnet; der gerade verwendete Stop wird außer Reichweite geparkt, solange die von ihm eröffnete Position lebt.
- Die Kerzen, beide Stop-Niveaus und jede Ausführung werden in einem Chartbereich gezeichnet, sodass sich die Ratsche direkt aus dem Bild ablesen lässt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs einer abgeschlossenen Kerze steigt über den oberen Trailing-Stop. Position modify im Modus Close kauft die Short-Position zurück, sofern eine offen ist, und Position modify im Modus Open kauft das Ordervolumen, sobald das Konto flat ist.
- **Short-Einstieg**: Der Schlusskurs einer abgeschlossenen Kerze fällt unter den unteren Trailing-Stop, während der Kurs noch unter dem oberen liegt. Dasselbe Paar arbeitet umgekehrt: Zuerst wird die Long-Position geschlossen, danach wird das Ordervolumen verkauft.
- **Ausstieg**: Es gibt keine eigene Ausstiegsregel. Eine Position wird gehalten, bis der gegenüberliegende Stop gebrochen wird, und dieser Bruch schließt sie und eröffnet zugleich die Gegenseite, sodass das Diagramm stets long, short oder eine Ausführung davon entfernt ist. Der Trailing-Stop zieht sich mit jedem neuen Fraktal enger, und genau das führt den Ausstiegskurs hinter einer offenen Position nach.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der einzigen Kerzenserie. Es werden nur abgeschlossene Kerzen geliefert, sodass einmal pro Bar entschieden wird. |
| Upper Fractal Length | 5 | Anzahl der Kerzen, über die das obere Extrem gemessen wird. Die geprüfte Kerze liegt in der Mitte dieses Fensters. |
| Lower Fractal Length | 5 | Dasselbe Fenster für das untere Extrem; halte es gleich dem oberen, sonst lesen die beiden Seiten Fraktale unterschiedlicher Breite. |
| Fractal Shift | 2 | Wie weit zurück die geprüfte Kerze liegt. Sie muss die Mitte des Fensters sein: zwei bei einem Fenster von fünf, drei bei einem Fenster von sieben. |
| Stop Buffer, % | 0 | Prozentsatz, der vor der Verwendung des Niveaus zum oberen Fraktalkurs addiert und vom unteren abgezogen wird. Null legt den Stop genau auf das Fraktal; ein größerer Wert hält ihn etwas weiter vom Kurs entfernt. |
| Order Volume | 1 | Ordergröße in Lots. Für beide Seiten wird dieselbe Größe verwendet, sodass eine Umkehr aus dem Schließen der alten Größe und dem anschließenden Eröffnen dieser Größe besteht. |

## Diagrammdetails

- Abgeschlossene Kerzen sind es, die die Niveaus vor dem Neuzeichnen bewahren. Der Indikatorblock behandelt jeden übergebenen Wert als endgültig, sodass eine noch laufende Kerze in Highest und Lowest geschrieben und bei deren nächster Aktualisierung überschrieben würde; werden nur abgeschlossene Kerzen geliefert, sehen die beiden Indikatoren nie einen Wert, der sich noch ändern kann.
- Die Fraktalprüfung lautet „größer oder gleich“ statt „größer“, sodass ein flaches Hoch, bei dem zwei Kerzen dasselbe Hoch teilen, weiterhin als Fraktal zählt, und auf der Unterseite gilt dasselbe für einen Doppelboden.
- Ein Fraktal steht konstruktionsbedingt erst zwei Kerzen später fest, und das Diagramm versucht nicht, das zu verbergen: Das erzeugte Niveau ist jenes, das vor zwei Bars galt, und es wird ab dem Bar verwendet, auf dem es bekannt wird.
- Solange eine Position offen ist, bleibt der Stop, der sie eröffnet hat, weit außer Reichweite geparkt und wird von dem ersten Fraktal wieder scharf gestellt, das sich nach dem Schließen der Position bildet. Ohne das würde das Diagramm weiterhin die Seite signalisieren, auf der es ohnehin schon steht, und die Ratsche würde das Niveau so weit vom Kurs wegziehen, dass es nie wieder durchbrochen werden könnte.
- Sollten beide Stops jemals im selben Moment als gebrochen gelten, hat die Long-Seite Vorrang: Die Short-Seite trägt die zusätzliche Bedingung, dass der Kurs noch unter dem oberen Stop liegt, sodass beide niemals gemeinsam auslösen können.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
