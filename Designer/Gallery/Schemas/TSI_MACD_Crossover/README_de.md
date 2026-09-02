# Diagramm der TSI-Signallinienkreuzung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der True Strength Index ist zweifach geglättetes Momentum: Er dreht spät, lügt aber selten. Gegen seine eigene exponentielle Signallinie gelesen verhält er sich wie ein langsamer MACD: Die Kreuzung nennt die Richtung, und der Abstand zwischen den Linien sagt, wie überzeugend die Wende ist. Dieses Diagramm nimmt nur Kreuzungen, bei denen dieser Abstand bereits ein Mindestmaß überschreitet, und trennt so den echten Wechsel der Kontrolle von zwei Linien, die einander nur streifen.

![schema](schema.svg)

## Strategieübersicht

- Ein einziger Baustein des True Strength Index trägt beide Linien; zwei Konverter holen aus demselben Wert die Indexlinie und ihre Signallinie heraus.
- Ein Kreuzungsbaustein vergleicht die beiden Linien und meldet die Richtung der Kreuzung; ein logisches NICHT macht aus derselben Ausgabe die Abwärtskreuzung.
- Eine Formel misst den absoluten Abstand zwischen den Linien, und ein Vergleich verlangt, dass er mindestens dem Mindestabstand entspricht, bevor die Kreuzung angenommen wird.
- Die Positionsprüfung entscheidet, ob ein Einstieg erlaubt ist, und das Ordervolumen ist das gemeinsame Volumen plus der Betrag der Position, sodass ein Gegensignal in einer Order dreht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die TSI-Linie kreuzt ihre Signallinie nach oben, der Abstand zwischen beiden erreicht mindestens den Mindestabstand und die Position ist nicht long. Die Order kauft das gemeinsame Volumen plus die Größe eines offenen Shorts, sodass eine einzige Marktorder den Short schließt und den Long eröffnet.
- **Short-Einstieg**: Die TSI-Linie kreuzt ihre Signallinie nach unten, der Abstand zwischen beiden erreicht mindestens den Mindestabstand und die Position ist nicht short. Die Order verkauft das gemeinsame Volumen plus die Größe eines offenen Longs.
- **Ausstieg**: Es gibt weder eine eigene Ausstiegsregel noch einen Schutzstopp, genau wie im Original: Die Position wird gehalten, bis die Gegenkreuzung sie dreht. Zwei Dinge sind vereinfacht. Das Original wartet nach jedem Einstieg zehn Kerzen, bevor es wieder auf Signale schaut, und kein Baustein hält einen Balkenzähler über Kerzen hinweg, deshalb entfällt diese Pause; die Positionsprüfung verhindert weiterhin einen zweiten Einstieg in dieselbe Richtung. Das Original schickt beim Drehen außerdem zwei Marktorders, was die Größe für einen Moment verdoppelt; hier erledigt die Volumenformel dasselbe in einer einzigen Order.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| TSI First Length | 25 | Erste Glättungsperiode des True Strength Index. |
| TSI Second Length | 13 | Zweite Glättungsperiode des True Strength Index. |
| TSI Signal Length | 7 | Periode der exponentiellen Signallinie, die über den Index gelegt wird. |
| Min spread | 2 | Mindestabstand zwischen Index und Signallinie, damit eine Kreuzung zählt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 01:00:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. Das Original läuft auf Vier-Stunden-Kerzen; in einem Monat Historie bleiben zu wenige abgeschlossene Balken, damit sich ein doppelt geglätteter Index bildet und danach noch handelt, deshalb ist das Diagramm auf Stundenkerzen herunterskaliert. |

## Diagrammdetails

- Der Kerzenbaustein speist den Baustein des True Strength Index, dessen komplexen Wert zwei Konverter in den Index und seine Signallinie zerlegen.
- Der Kreuzungsbaustein erhält den Index am oberen und die Signallinie am unteren Eingang, sodass seine Ausgabe bei einer Aufwärtskreuzung wahr und bei einer Abwärtskreuzung falsch ist.
- Abstandsformel und Vergleich rechnen auf jeder Kerze, der Kreuzungsbaustein meldet sich dagegen nur bei Kreuzungen, sodass jedes logische UND genau auf dem Balken auslöst, auf dem eine gefilterte Kreuzung stattfindet.
- Beide Bausteine zur Positionsänderung beziehen ihr Volumen aus einer Formel, die den Betrag der Position zur gemeinsamen Volumenkonstante addiert.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
