# Diagramm der Strategie Night Session Bollinger Fade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Band zeigt, wo der Kurs aufgehört hat, sich gewöhnlich zu verhalten; eine Uhr zeigt, wann es sich lohnt, darauf zu reagieren. Dieses Diagramm bringt beides auf abgeschlossenen Stundenkerzen zusammen: Es handelt gegen die Berührung eines Bollinger-Bands, aber nur in den Abendstunden und nur solange der Kanal selbst schmal ist. Der Ausstieg ist die Mittellinie, und diese Hälfte ist nicht an die Uhr gebunden – eine spät am Abend eröffnete Position wird geschlossen, sobald der Kurs zurückkommt, zu welcher Stunde das auch geschieht.

![schema](schema.svg)

## Strategieübersicht

- Eine einzige Kerzenserie treibt alles an. Es werden nur abgeschlossene Stundenkerzen ausgegeben, sodass jeder Vergleich, jede Bedingung und jede Order auf einer geschlossenen Kerze entschieden wird.
- Bollinger Bands über 20 Kerzen mit einer Abweichung von 2.0 liefern erst Werte, sobald sie formiert sind. Drei Converter-Blöcke zerlegen den Indikatorwert in oberes Band, unteres Band und Mittellinie.
- Eine Formula subtrahiert das untere Band vom oberen und erhält so die Kanalbreite, und ein Comparison hält sie gegen Width Threshold. Diese eine Antwort ist die Ruhe-Bedingung, die sich beide Einstiege teilen.
- Der Block Working time liest den Zeitstempel, den jede Kerze trägt – ihre Eröffnungszeit – und antwortet von 19:00:00 bis 23:59:59 mit true. Nur die beiden Einstiegsbedingungen fragen ihn ab.
- Ein Long wird angenommen, wenn vier Antworten auf derselben Kerze übereinstimmen: Das Tief hat das untere Band erreicht, der Kanal liegt innerhalb des Schwellenwerts, die Kerze wurde innerhalb der Session eröffnet, und die Position ist flat.
- Ein Short ist das Spiegelbild: Das Hoch hat das obere Band erreicht, unter denselben Bedingungen für Breite, Session und flache Position.
- Beide Einstiege sind Market-Orders, die Position modify unter der Bedingung Open position sendet, sodass ein Signal, das bei bereits offener Position eintrifft, vom Order-Block selbst abgelehnt wird und nicht durch einen zusätzlichen Vergleich.
- Die Ausstiege vergleichen den Schlusskurs mit der Mittellinie und werden in beiden Richtungen als Reduce only-Orders gesendet. Das Chart-Panel zeichnet die Kerzen, die drei Bandlinien sowie Orders und Ausführungen aller vier Order-Blöcke.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Innerhalb des Abendfensters kauft Position modify auf einer abgeschlossenen Kerze, deren Tief das untere Band erreicht oder unterschritten hat, bei einer Kanalbreite nicht größer als der Schwellenwert und flacher Position, das Ordervolumen zum Marktpreis unter der Bedingung Open position.
- **Short-Einstieg**: Innerhalb desselben Fensters verkauft Position modify auf einer abgeschlossenen Kerze, deren Hoch das obere Band erreicht oder überschritten hat, bei einer Kanalbreite nicht größer als der Schwellenwert und flacher Position, das Ordervolumen zum Marktpreis unter der Bedingung Open position.
- **Ausstieg**: Die Mittellinie ist das Ziel für beide Seiten. Auf jeder abgeschlossenen Kerze, die auf oder über ihr schließt, wird ein Reduce only-Verkauf gesendet, und auf jeder Kerze, die auf oder unter ihr schließt, ein Reduce only-Kauf. Ein Reduce only-Block, dem die Richtung übergeben wird, die die Position ohnehin schon hält, lehnt die Order von sich aus ab, sodass der Verkaufspfad bei einer Short-Position stumm bleibt und der Kaufpfad bei einer Long-Position. Keiner der beiden Ausstiege ist durch die Session oder durch die Kanalbreite begrenzt – sie laufen auf jeder abgeschlossenen Kerze, rund um die Uhr. Es gibt weder Stop-Loss noch Take-Profit: Die Rückkehr zur Mittellinie ist der einzige Weg aus einem Trade.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 01:00:00 | Zeitrahmen der einzigen Kerzenserie, auf der das gesamte Diagramm läuft; nur abgeschlossene Kerzen verlassen ihn. |
| Bollinger Period | 20 | Anzahl der Kerzen, über die die Bänder gemittelt werden. |
| Bollinger Deviation | 2.0 | Standardabweichungs-Multiplikator, der festlegt, wie weit die beiden Bänder von der Mittellinie entfernt liegen. |
| Width Threshold | 3000 | Breitester Kanal in Preiseinheiten des Instruments, der noch als ruhig genug für einen Einstieg gilt. |
| Session From | 19:00:00 | Beginn des Fensters, in dem Einstiege erlaubt sind; verglichen wird mit der Eröffnungszeit der Kerze. |
| Session Until | 23:59:59 | Ende dieses Fensters; eine Kerze, die genau zu diesem Zeitpunkt oder davor eröffnet, zählt noch als innerhalb. |
| Order Volume | 0.01 | Ordergröße, die von beiden Einstiegen und von beiden Ausstiegen gesendet wird. |

## Diagrammdetails

- Der Einstieg liest die Extremwerte der Kerze und nicht ihren Schlusskurs. Eine Kerze, die ein Band innerhalb der Kerze durchstochen hat und wieder innerhalb geschlossen hat, zählt weiterhin als Berührung – genau das macht daraus ein Fade der Übertreibung statt eines Ausbruchs des Schlusskurses.
- Die Breiten-Bedingung wird in Preiseinheiten des Instruments gemessen, nicht in Prozent. Bei einem Instrument, das in Einheiten notiert, passiert sie fast jede Kerze und die Bedingung ist praktisch offen; bei einem, das in Zehntausenden notiert, wird sie zu dem selektiven Filter, der sie sein soll. Der Schwellenwert ist nach außen geführt, damit er an das Instrument angepasst werden kann.
- Beide Ausstiege tragen eine Richtung, obwohl Reduce only die Seite selbst bestimmt: Erst die Richtung lässt jeden Pfad die falsche Position ablehnen. Deshalb erscheint auf der Ausstiegsseite des Diagramms kein Long- oder Short-Vergleich – diese Prüfung übernehmen die beiden Order-Blöcke.
- Eine einzige Variable Volume speist alle vier Order-Blöcke. Bei den Ausstiegen kürzt Reduce only die Order auf das, was die Position tatsächlich hält, sodass selbst ein nur teilweise ausgeführter Einstieg exakt geschlossen und nicht gedreht wird.
- Die Session wird je Kerze ausgewertet und nicht anhand einer frei laufenden Uhr, sodass sie ihren Wert im selben Takt ändert wie die Band- und Breitenantworten und die vier Eingänge einer Einstiegsbedingung stets dieselbe Kerze beschreiben.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
