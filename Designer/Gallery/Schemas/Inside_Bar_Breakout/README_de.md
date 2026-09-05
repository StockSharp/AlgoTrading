# Diagramm der Inside-Bar-Ausbruchsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Inside Bar ist eine Kerze, deren gesamte Spanne in die der vorangegangenen Kerze passt: Käufer und Verkäufer drücken nicht mehr, der Markt ist zusammengezogen. Das Diagramm wartet darauf, dass die unmittelbar folgende Kerze diese Spanne verlässt, und nimmt den Ausbruch in Richtung des Verlassens mit; danach führt eine einfache gleitende Durchschnittslinie den Trade und entscheidet, wann die Bewegung vorbei ist.

![schema](schema.svg)

## Strategieübersicht

- Zwei Kerzenmuster-Bausteine tragen je eine Formel über drei Kerzen: eine freie erste Kerze, einen streng darin liegenden Inside Bar und eine Ausbruchskerze.
- Die Long-Formel verlangt von der Ausbruchskerze ein Hoch über dem Hoch des Inside Bar, die Short-Formel ein Tief unter dessen Tief.
- Der einfache gleitende Durchschnitt der Schlusskurse ist der einzige Indikator: Am Einstieg ist er nicht beteiligt und dient rein als Ausstiegslinie.
- Die Positionsprüfung sorgt dafür, dass ein Ausbruch nur aus der Neutralstellung gehandelt wird, ein Muster ergibt also genau einen Trade.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Musterbaustein meldet einen Inside Bar, dessen Hoch die folgende Kerze soeben überschritten hat, und die Position ist neutral. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Der Musterbaustein meldet einen Inside Bar, dessen Tief die folgende Kerze soeben unterschritten hat, und die Position ist neutral. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird geschlossen, sobald eine Kerze unter dem gleitenden Durchschnitt schließt, ein Short, sobald sie darüber schließt, beides über Bausteine zur Positionsänderung im Schließmodus. Der Musterbaustein sieht nur ein Fenster fester Länge, daher muss der Ausbruch auf der Kerze unmittelbar nach dem Inside Bar kommen; spätere Ausbrüche werden ignoriert.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts, der die Trades schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Musterbausteine, den gleitenden Durchschnitt und einen Konverter, der den Schlusskurs aus der Kerze holt.
- Jeder Musterbaustein enthält drei Formeln, eine je Kerze des Musters, und meldet nur auf der Kerze wahr, die den Ausbruch vollendet.
- Der Positionsbaustein wird mit einer Nullkonstante verglichen, und jedes logische UND verbindet diesen Schutz mit einem der beiden Ausbruchssignale.
- Beide Einstiegsbausteine senden Marktorders und beziehen ihr Volumen aus einer gemeinsamen Konstante; die beiden Ausstiegsbausteine werden direkt von den Vergleichen mit dem Durchschnitt ausgelöst und greifen nur, wenn es etwas zu schließen gibt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
