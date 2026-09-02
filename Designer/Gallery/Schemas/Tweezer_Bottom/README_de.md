# Diagramm der Pinzetten-Boden-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Pinzette besteht aus zwei benachbarten Kerzen, die sich auf demselben Niveau gegeneinander drehen: Nach einer fallenden Kerze bleibt eine steigende fast auf demselben Tief stehen, und das Paar markiert einen Boden. Das Spiegelbild an den Hochs markiert eine Decke. Da zwei Tiefs praktisch nie auf den Tick genau übereinstimmen, misst das Diagramm ihren Abstand in Prozent und lässt sie als gleich gelten, solange dieser Abstand unter der Toleranz bleibt.

![schema](schema.svg)

## Strategieübersicht

- Ein Kerzenmuster-Baustein erkennt nur den Farbwechsel des Paares: fallende Kerze und danach steigende für den Boden, steigende und danach fallende für die Decke.
- Die Gleichheit der Extreme misst eine eigene Formel, sodass die Toleranz ein optimierbarer Schemaparameter bleibt und nicht im Mustertext eingefroren wird.
- Der einfache gleitende Durchschnitt ist am Einstieg unbeteiligt; er entscheidet nur, wann der Trade vorbei ist.
- Jeder Einstieg ist durch die Position abgesichert, sodass eine Pinzette ein Wendeversuch bleibt und nie eine laufende Position vergrößert.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Musterbaustein meldet eine fallende Kerze gefolgt von einer steigenden, der Abstand der beiden Tiefs liegt höchstens bei der Toleranz in Prozent des vorigen Tiefs und die Position ist neutral. Die Order kauft das gemeinsame Volumen zum Marktpreis.
- **Short-Einstieg**: Der Musterbaustein meldet eine steigende Kerze gefolgt von einer fallenden, der Abstand der beiden Hochs liegt höchstens bei der Toleranz in Prozent des vorigen Hochs und die Position ist neutral. Die Order verkauft das gemeinsame Volumen zum Marktpreis.
- **Ausstieg**: Die erste Kerze, die unter dem gleitenden Durchschnitt schließt, beendet einen Long, die erste darüber einen Short; beide Ausstiege sind Bausteine zur Positionsänderung im Schließmodus und eröffnen nie etwas. Das Original kennt weder Stop-Loss noch Take-Profit, dieses Diagramm ebenso wenig. Zwei Dinge aus dem Original ließen sich mit den vorhandenen Bausteinen nicht abbilden: die Pause von fünfhundert Balken nach jedem Trade, weil kein Baustein einen Zähler über Kerzen hinweg hält, und die Minutenzeiteinheit, die auf die Fünf-Minuten-Kerzen der mitgelieferten Historie skaliert wurde.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Tolerance, % | 0.1 | Wie weit die beiden Extreme auseinanderliegen dürfen, in Prozent des Niveaus der vorigen Kerze. |
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts, der die Trades schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Musterbausteine, den gleitenden Durchschnitt und drei Konverter für Tief, Hoch und Schluss.
- Zwei Bausteine für den vorigen Wert halten Tief und Hoch der vorherigen Kerze, und zwei Formeln machen aus jedem Paar den prozentualen Abstand der Extreme.
- Zwei Vergleiche prüfen diese Abstände gegen die gemeinsame Toleranzkonstante, ein weiterer prüft die Position gegen null.
- Jedes logische UND verbindet Muster, übereinstimmende Extreme und Neutralprüfung und löst dann einen Einstiegsbaustein aus, der sein Volumen aus der gemeinsamen Konstante bezieht.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
