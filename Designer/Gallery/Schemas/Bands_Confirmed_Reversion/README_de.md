# Strategiediagramm Bands Confirmed Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Kerze, die außerhalb eines Volatilitätsbandes eröffnet und wieder innerhalb davon schließt, ist das klassische Bild einer abgewiesenen Bewegung – und genau dieses Bild kauft und verkauft dieses Diagramm. Mehr als ein Ein-Kerzen-Muster wird daraus durch das, was zwischen Muster und Order steht: ein Preiskanal, dessen Rand sich behauptet haben muss, und ein Zählbaustein, der seine Bestätigung erst freigibt, wenn eine festgelegte Anzahl von Kerzen vergangen ist. Erst wenn Muster, Kanal und Zählung auf derselben Kerze zusammentreffen, wird eine Position eröffnet.

![schema](schema.svg)

## Strategieübersicht

- Eine einzige Fünfzehn-Minuten-Kerzenreihe versorgt das gesamte Diagramm, und jeder eintretende Wert läuft zuerst durch einen Final-Baustein, sodass nachgelagert nie eine noch entstehende Kerze zu sehen ist.
- Drei Indikatoren arbeiten auf diesem Strom: Volatilitätsbänder um einen gleitenden Durchschnitt, ein Preiskanal aus Hochs und Tiefs sowie eine Average True Range, die misst, wie breit eine normale Kerze ist.
- Konverter zerlegen die Bänder in eine obere und eine untere Linie, den Kanal in einen oberen und einen unteren Rand und entnehmen jeder abgeschlossenen Kerze Eröffnung, Schluss, Hoch und Tief.
- Zwei Previous value-Bausteine halten die Kanalränder der vorangegangenen Kerze fest, und zwei Vergleiche fragen, ob der untere Rand aufgehört hat zu fallen und ob der obere Rand aufgehört hat zu steigen: Das ist die Definition eines haltenden Kanals in diesem Diagramm.
- Jede dieser beiden Antworten schaltet einen N values-Baustein scharf, der anschließend die eingestellte Anzahl abgeschlossener Kerzen zählt und einen einzelnen Bestätigungsimpuls freigibt; ein erneutes Scharfschalten wird ignoriert, solange eine Zählung läuft.
- Eine logische Bedingung führt für die Long-Seite fünf Dinge zusammen: Die Kerze hat unterhalb des unteren Bandes eröffnet, sie hat wieder oberhalb davon geschlossen, der Kanalboden hält in diesem Moment, der Bestätigungsimpuls ist soeben eingetroffen, und die Position ist flat. Die Short-Seite ist dieselbe Bedingung, gespiegelt am oberen Band und am oberen Kanalrand.
- Beide Einstiege sind Market-Orders über Position modify-Bausteine, die nur aus einer flachen Position heraus eröffnen; ein Signal, das während eines laufenden Trades eintrifft, kann so keine zweite Position obendrauf setzen.
- Zwei Combination-Bausteine sammeln die Ausstiegsgründe – einen aus der Average True Range gebildeten Stop-Abstand und einen Schluss jenseits des Kanals der vorangegangenen Kerze – und geben sie an die schließenden Position modify-Bausteine weiter, während ein Chart-Panel die Kerzen, alle drei Indikatoren, die Orders und die Ausführungen zeichnet.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Eine abgeschlossene Kerze hat unterhalb des unteren Bandes eröffnet und wieder oberhalb davon geschlossen, der Kanalboden liegt auf oder über dem Stand der vorangegangenen Kerze, der Long-Zählbaustein hat soeben seine Bestätigung freigegeben, und die Position ist flat. Der Position modify-Baustein kauft dann das Ordervolumen zum Markt. Die Zählung ist es, die die Trades auseinanderzieht: Nach jeder Freigabe schaltet sich der Baustein bei der nächsten Kerze wieder scharf, deren Kanalboden weiterhin hält, sodass dasselbe Muster auf der unmittelbar folgenden Kerze keinen zweiten Einstieg erzeugt.
- **Short-Einstieg**: Das Spiegelbild. Eine abgeschlossene Kerze hat oberhalb des oberen Bandes eröffnet und wieder unterhalb davon geschlossen, der obere Kanalrand liegt auf oder unter dem Stand der vorangegangenen Kerze, der Short-Zählbaustein hat seine Bestätigung freigegeben, und die Position ist flat. Der Position modify-Baustein verkauft das Ordervolumen zum Markt.
- **Ausstieg**: Jede Seite hat ihren eigenen Combination-Baustein, der zwei Arten von Gründen zusammenfasst. Der erste ist ein Volatilitäts-Stop: Die Long-Position wird geschlossen, wenn das Tief der Kerze unter das untere Band minus Average True Range mal Stop-Multiplikator fällt, und die Short-Position wird geschlossen, wenn das Hoch der Kerze über das obere Band plus denselben Abstand steigt. Da sich das Band mit dem Markt bewegt, zieht dieser Stop von selbst nach, ohne dass sich das Diagramm einen Einstiegskurs merken müsste. Der zweite Grund ist ein Kanalausbruch: Ein Schluss über dem oberen Kanalrand der vorangegangenen Kerze oder ein Schluss unter dem unteren Kanalrand der vorangegangenen Kerze beendet den Trade auf beiden Seiten – nach oben nimmt er den Gewinn aus einer Long-Position, nach unten den Verlust, und umgekehrt bei einer Short-Position. Beide schließenden Bausteine sind auf Schließen der Position eingestellt, sodass jeder nur auf der Seite wirkt, zu der er gehört, und stets die gesamte Größe sendet.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:15:00 | Zeitrahmen der einzigen Kerzenreihe, auf der das gesamte Diagramm läuft. Ein kürzerer Zeitrahmen liefert mehr Muster und mehr Trades, ein längerer weniger und langsamere. |
| Bollinger Length | 100 | Anzahl der Kerzen, über die die Volatilitätsbänder gemittelt werden. Sie legt zugleich die Aufwärmphase fest: Es wird erst gehandelt, wenn so viele Kerzen vergangen sind. |
| Bollinger Width | 1 | Wie viele Standardabweichungen jedes Band vom Durchschnitt entfernt liegt. Hier bewusst eng gehalten, damit Kerzen regelmäßig außerhalb eines Bandes eröffnen und wieder innerhalb schließen; für seltenere und extremere Abweisungen den Wert vergrößern. |
| Donchian Length | 100 | Anzahl der Kerzen, über die sich der Preiskanal erstreckt. Ein langer Kanal macht seine Ränder träge, und genau das macht aus „der Rand bewegt sich nicht mehr gegen uns“ einen aussagekräftigen Filter. |
| ATR Length | 21 | Anzahl der Kerzen, über die die Average True Range gemessen wird. Sie legt die Einheit fest, in der der Stop-Abstand ausgedrückt wird. |
| Long Confirm Candles | 5 | Kerzen, die der Long-Zählbaustein zwischen Scharfschalten und Freigabe seiner Bestätigung wartet. Bei einer Kerze entfällt die Wartezeit vollständig und jedes Muster wird gehandelt; größere Werte dünnen die Einstiege aus. |
| Short Confirm Candles | 5 | Kerzen, die der Short-Zählbaustein wartet. Es ist eine eigene Einstellung, damit sich die beiden Seiten gegeneinander abstimmen lassen. |
| ATR Stop Multiplier | 2 | Um wie viele Average True Ranges der Long-Stop unterhalb des unteren Bandes und der Short-Stop oberhalb des oberen Bandes liegt. Kleinere Werte ergeben engere, häufigere Ausstiege. |
| Order Volume | 1 | Ordergröße in Lots, die beim Einstieg gesendet wird. Die Ausstiege schließen stets das, was offen ist, und haben keine eigene Größe. |

## Diagrammdetails

- Der Kerzenbaustein ist auf ausschließlich abgeschlossene Kerzen eingestellt, und der dahinterliegende Final-Baustein setzt dieselbe Regel innerhalb des Diagramms durch. Ein Update einer noch entstehenden Kerze trägt die Eröffnungszeit des Bars, und eine aus einem solchen Wert gebildete Order ist gegenüber der Uhr rückdatiert und wird abgelehnt; deshalb bleibt die gesamte Logik auf abgeschlossenen Kerzen.
- Der N values-Baustein zählt die Werte, die ihn nach dem Scharfschalten erreichen; er prüft nicht, ob die auslösende Bedingung durchgehend erfüllt blieb. Deshalb ist derselbe Kanalvergleich, der ihn scharf schaltet, zusätzlich direkt in die Einstiegsbedingung verdrahtet: Der Impuls sagt, dass die Wartezeit vorbei ist, und der aktuelle Vergleich sagt, ob ihr Grund noch besteht.
- Die für den Ausstieg genutzten Kanalränder werden eine Kerze zurück genommen. Der aktuelle obere Rand eines Kanals aus Hochs und Tiefs enthält bereits das Hoch der laufenden Kerze, sodass ein Schluss ihn nie überschreiten kann; gegenüber dem Rand der vorangegangenen Kerze ist ein Ausbruch dagegen ein echtes Ereignis.
- Der Stop ist am Volatilitätsband verankert und nicht an dem Kurs, zu dem der Trade eröffnet wurde. Ein Diagramm hat keine Erinnerung an einen Einstiegskurs, solange nicht eine Variable dafür ergänzt wird, und am Band ist der Einstieg ohnehin erfolgt; so ergibt sich derselbe Schutzabstand, und er wandert mit dem Markt mit.
- Die Position ist bewusst doppelt abgesichert: Der Flat-Vergleich steckt in der Einstiegsbedingung, und die Einstiegsbausteine selbst sind so eingestellt, dass sie nur bei einer Position von null eröffnen. Das Erste hält das Diagramm lesbar, das Zweite verhindert tatsächlich eine doppelte Order, wenn beide nicht im Gleichtakt eintreffen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
