# Diagramm der Strategie Money Target Flatten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Geldziel schaut überhaupt nicht auf den Kurs: Es schließt die Position, sobald das Geld in ihr einen bestimmten Betrag erreicht. Dieses Diagramm setzt diese Regel auf einen schlichten Motor aus zwei Durchschnitten. Die Kreuzung entscheidet, wann man im Markt ist; das Gewinnziel und die Verlustgrenze entscheiden, wann es genug ist, und sobald eines von beiden erreicht wird, wird die Position glattgestellt und jede noch aktive Order gleich mit storniert.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen speisen einen schnellen und einen langsamen exponentiellen gleitenden Durchschnitt, und ein Kreuzungsblock macht aus dem Paar ein einziges Ereignis: wahr, wenn die schnelle Linie die langsame nach oben durchbricht, falsch, wenn sie sie nach unten durchbricht.
- Ein logisches NICHT gibt der Abwärtskreuzung ein eigenes Signal, sodass jede Richtung ein eigenständiges Gatter besitzt.
- Der Positionsblock, verglichen mit null, sagt, ob das Diagramm ohne Position, long oder short ist, und jedes Gatter ist ein logisches UND aus einer Kreuzung und einem Positionszustand.
- Beide Einstiege sind Market-Orders mit festem Volumen und tragen die Bedingung zum Positionsaufbau, sodass eine Kreuzung, die bei bereits laufender Position eintrifft, diese nicht aufstocken kann.
- Der Strategy-P&L-Block liefert das offene Ergebnis der laufenden Position in einem eigenen Takt, mehrmals pro Stunde statt einmal je Bar.
- Zwei Vergleiche halten diese Zahl gegen das Gewinnziel und gegen die Verlustgrenze, und jede Schwelle ist eine Variable, die vom selben P&L-Wert ausgelöst wird, sodass ein Vergleich beide Operanden stets aus demselben Moment sieht.
- Ein Combination-Block führt die beiden Antworten zu einer einzigen Ausstiegslinie zusammen, die zwei Dinge zugleich tut: Position modify schließt die Position zum Marktpreis, und Mass order cancellation räumt alles ab, was noch aktiv ist.
- Eine Kreuzung, die gegen eine offene Position läuft, schließt diese ebenfalls, sodass das Diagramm nie in einem Trade sitzt, gegen den sich die Durchschnitte bereits gedreht haben.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der schnelle Durchschnitt kreuzt auf einer abgeschlossenen Kerze über den langsamen, während keine Position offen ist: Position modify kauft das Ordervolumen zum Marktpreis.
- **Short-Einstieg**: Der schnelle Durchschnitt kreuzt auf einer abgeschlossenen Kerze unter den langsamen, während keine Position offen ist: Position modify verkauft das Ordervolumen zum Marktpreis.
- **Ausstieg**: Zwei voneinander unabhängige Ausstiege. Der Geldausstieg löst aus, sobald das offene Ergebnis der Position das Gewinnziel erreicht oder auf die Verlustgrenze fällt: Der Combination-Block gibt das Signal weiter, die Position wird zum Marktpreis geschlossen und jede verbliebene Order wird storniert. Eine Kreuzung gegen die offene Position schließt diese ebenfalls. Was zuerst eintritt, stellt das Diagramm glatt; es wartet, und die nächste Kreuzung, die es ohne Position antrifft, eröffnet die nächste Position, long oder short.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzenreihe, auf der beide Durchschnitte aufgebaut werden. |
| Fast EMA Length | 12 | Länge des schnellen exponentiellen gleitenden Durchschnitts. |
| Slow EMA Length | 26 | Länge des langsamen exponentiellen gleitenden Durchschnitts. |
| Volume | 1 | Ordergröße in Lots, die jeder Einstieg sendet. |
| Profit To Close | 300 | Offenes Ergebnis in Geld, bei dem die Position als Gewinn geschlossen wird. |
| Loss To Close | -600 | Offenes Ergebnis in Geld, bei dem die Position als Verlust geschlossen wird; es wird als negative Zahl angegeben, weil es direkt mit dem Ergebnis verglichen wird. |

## Diagrammdetails

- Die Schwellen werden mit dem unrealisierten Ergebnis verglichen, also mit dem Geld in der laufenden Position, und wirken damit als Geld-Take-Profit und Geld-Stop-Loss für jede Position einzeln. Dieselben zwei Vergleiche, an den realisierten Ausgang gelegt, machen aus dem Paar dagegen einen Einwegschalter: Sobald das Konto den Betrag erreicht, ist der Lauf beendet.
- Der Geldausstieg ist nicht an den Kerzentakt gebunden. Er reagiert auf die P&L-Aktualisierung, sodass ein Ziel mitten in einer Bar mitgenommen werden kann und nicht erst zum nächsten Schluss.
- Jede Order, die das Diagramm sendet, ist eine Market-Order, weshalb das Abräumen auf den mitgelieferten historischen Daten nichts zu stornieren findet. Es ist dennoch verdrahtet, weil ein Geldausstieg, der aktive Orders zurücklässt, nur ein halber Ausstieg ist, und es zählt in dem Moment, in dem eine im Buch liegende Order zum Diagramm hinzukommt.
- Beide Schließblöcke tragen die Bedingung zum Schließen der Position und brauchen daher weder eine Richtung noch ein Volumen: Der Block liest die offene Position und sendet die Gegenorder über genau diese Größe.
- Erst der Fünf-Minuten-Takt macht die Geldebene sichtbar. Auf einer deutlich längeren Kerze kreuzt dieses Durchschnittspaar nur eine Handvoll Mal im Monat, es gibt wenige Positionen, und die Schwellen sind auf die Schwankung zugeschnitten, die eine Fünf-Minuten-Reihe hervorbringt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
