# Diagramm der Strategie Session Window Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Ausbruch lohnt sich nur, solange es jemanden gibt, gegen den gehandelt werden kann. Dieses Diagramm misst die Spanne über zwanzig Kerzen, eröffnet eine Position, wenn eine abgeschlossene Kerze außerhalb dieser Spanne schließt, und wird überhaupt nur tätig, wenn diese Kerze in ein festes Zeitfenster des Tages fällt. Alles nach dem Einstieg bleibt einem prozentualen Stop und Take überlassen.

![schema](schema.svg)

## Strategieübersicht

- Eine einzige Fünf-Minuten-Kerzenreihe treibt das gesamte Diagramm an, und es werden nur abgeschlossene Kerzen veröffentlicht; jede Entscheidung fällt also auf einer Kerze, die sich nicht mehr ändern kann.
- Highest und Lowest, beide über zwanzig Kerzen und beide nur im ausgebildeten Zustand, tragen die obere und die untere Grenze der jüngsten Spanne.
- Previous value verschiebt jede Grenze um eine Kerze zurück. Erst diese Verschiebung macht aus einer Spanne ein Ausbruchsniveau: Die geprüfte Kerze darf nicht Teil der Grenze sein, die sie überwinden muss.
- Ein Konverter liest den Schlusskurs der aktuellen Kerze aus, und zwei Vergleiche stellen ihn den beiden verschobenen Grenzen gegenüber.
- Working time beantwortet pro Kerze eine einzige Frage – gehört diese Kerze in das Handelsfenster – und liefert im selben Takt wie die Vergleiche ein schlichtes Wahr oder Falsch.
- Zwei logische Bedingungen verknüpfen Ausbruch und Zeitfenster; ein Niveaubruch außerhalb des Fensters bewirkt daher gar nichts, und das Diagramm bleibt den Rest des Tages untätig.
- Beide Einstiege sind Market-Orders mit festem Volumen und tragen beide die Bedingung Open position; eine Order wird daher nur aus einer flachen Position heraus gesendet, niemals zum Aufstocken oder Umkehren einer bestehenden.
- Position protection übernimmt die Ausführungen des Einstiegs und den Schlusskurs der Kerze und führt den Trade von da an; geschlossen wird bei einem festen Prozentsatz an Gewinn oder Verlust.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Eine abgeschlossene Kerze schließt über dem Zwanzig-Kerzen-Hoch, das um eine Kerze zurückversetzt ist, und diese Kerze fällt in das Handelsfenster. Position modify kauft das Ordervolumen zum Marktpreis; die Bedingung Open position lässt die Order nur durch, solange die Position flach ist.
- **Short-Einstieg**: Eine abgeschlossene Kerze schließt unter dem Zwanzig-Kerzen-Tief, das um eine Kerze zurückversetzt ist, unter derselben Fensterbedingung. Position modify verkauft das Ordervolumen zum Marktpreis, ebenfalls nur aus einer flachen Position heraus.
- **Ausstieg**: Im Diagramm gibt es kein Ausstiegssignal. Sobald eine Position offen ist, führt Position protection sie: Der Trade wird ab der Einstiegsausführung bepreist, folgt dem Schlusskurs der Kerze und wird bei 1.5% Gewinn oder 0.5% Verlust geschlossen – ein Ziel, das dem Dreifachen des Risikos entspricht. Das Zeitfenster steuert ausschließlich die Einstiege, sodass eine ganz am Ende des Fensters eröffnete Position über das Ende des Fensters hinaus bestehen bleibt, bis eine ihrer beiden Grenzen erreicht ist. Ein gegenläufiger Ausbruch in der Zwischenzeit wird ignoriert, weil die Bedingung Open position jeden Einstieg blockiert, der nicht aus einer flachen Position erfolgt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzenreihe; nur abgeschlossene Kerzen erreichen das Diagramm. |
| Breakout High Length | 20 | Anzahl der Kerzen, über die die obere Grenze gemessen wird, bevor die Verschiebung um eine Kerze angewendet wird. |
| Breakout Low Length | 20 | Anzahl der Kerzen, über die die untere Grenze gemessen wird. Halten Sie den Wert gleich der oberen Länge, damit beide Grenzen dieselbe Spanne beschreiben. |
| Session From | 12:00:00 | Beginn des Handelsfensters als Tageszeit. Eine Kerze, die davor eröffnet wurde, kann keinen Einstieg auslösen. |
| Session Until | 21:00:00 | Ende des Handelsfensters. Eine Kerze, die danach eröffnet wurde, kann keinen Einstieg auslösen; eine bereits offene Position ist davon nicht betroffen. |
| Order Volume | 1 | Feste Stückzahl, die von beiden Market-Einstiegen gesendet wird. |
| Take Profit, % | 1.5 | Abstand des Take-Profits in Prozent des Einstiegskurses. |
| Stop Loss, % | 0.5 | Abstand des Stop-Loss in Prozent des Einstiegskurses. |

## Diagrammdetails

- Beide Spannen-Indikatoren arbeiten nur im ausgebildeten Zustand und nur mit endgültigen Werten; kein unfertiger Wert kann daher eine Grenze verschieben, und die ersten zwanzig Kerzen erzeugen überhaupt kein Signal.
- Die Verschiebung um eine Kerze wird auf die Ausgabe des Indikators angewendet, nicht auf den Kurs. Ein um einen Schritt zurückversetzter Indikatorwert ist genau die Grenze, wie sie vor dem Entstehen der aktuellen Kerze bestand – und daran muss ein Ausbruch gemessen werden.
- Working time liest den Zeitstempel aus, den der übergebene Wert trägt. Eine Kerze trägt ihre Eröffnungszeit; eine Kerze gilt daher als innerhalb des Fensters, wenn sie innerhalb des Fensters eröffnet wurde.
- Die Ausführungen beider Einstiege werden zu einem Strom zusammengeführt, bevor sie Position protection erreichen; ein einziger Schutzbaustein deckt damit Long und Short gleichermaßen ab.
- Position protection erhält den Schlusskurs der Kerze als Preis; Stop-Loss und Take-Profit werden dadurch an denselben Kursen abgeschlossener Kerzen gemessen, auf denen auch die Einstiegsentscheidung beruht, und einmal je Kerze geprüft.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
