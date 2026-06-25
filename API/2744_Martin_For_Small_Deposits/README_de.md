# Martin Für Kleine Einlagen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert den Averaging-Experten "Martin for small deposits" in StockSharp. Sie analysiert 15 abgeschlossene Kerzen und eröffnet eine Position nur, wenn der neueste Schluss (für Longs) unter oder (für Shorts) über dem 14 Balken zuvor aufgezeichneten Schluss liegt. Alle Trades werden zu Marktpreisen über die High-Level-Strategie-API ausgeführt, und die Logik wird einmal pro abgeschlossener Kerze angewendet.

## Einstiegslogik
- Ein rollender Puffer hält die letzten 15 abgeschlossenen Kerzen-Schlüsse.
- Wenn keine offenen oder ausstehenden Positionen vorhanden sind, vergleicht die Strategie den jüngsten Schluss mit dem Schluss 14 Balken zuvor.
- Wenn der letzte Schluss niedriger ist, wird ein Long-Grid gestartet; wenn er höher ist, wird ein Short-Grid gestartet.
- Das Trade-Volumen für die erste Order entspricht **Initial Volume**. Nachfolgende Einstiege auf derselben Seite verwenden den Martingal-Multiplikator, bevor sie auf den Volumenschritt des Instruments normalisiert werden.

## Positionsverwaltung
- Während eine Position besteht, wartet die Strategie auf **Bars To Skip** abgeschlossene Kerzen, bevor sie einen weiteren Averaging-Trade in Betracht zieht.
- Zusätzliche Orders werden nur gesendet, wenn sich der Preis um mindestens **Step (pips)** gegen die aktuelle Richtung bewegt, in Preiseinheiten über die erkannte Pip-Größe umgerechnet.
- Jede Ausführung aktualisiert interne Statistiken: aggregiertes Volumen, durchschnittlicher Einstiegspreis, niedrigster (für Longs) oder höchster (für Shorts) Einstiegspreis und der Preis des jüngsten Füllers.
- Das Volumen überschreitet nie **Max Volume** oder das von der Exchange definierte maximale Volumen. Wenn die normalisierte Größe unter das minimal erlaubte Volumen fällt, wird die Order übersprungen.

## Ausstiegsbedingungen
- Wenn der unrealisierte Nettogewinn (Differenz zwischen dem aktuellen Schluss und dem durchschnittlichen Einstiegspreis, multipliziert mit dem Positionsvolumen) **Min Profit** überschreitet, werden alle offenen Orders abgeflacht.
- Wenn **Take Profit (pips)** größer als null ist und der Preis diese Distanz vom letzten Einstieg in der günstigen Richtung erreicht, wird das gesamte Grid geschlossen.
- Schließungsanfragen werden verfolgt; keine neuen Orders werden gesendet, bis Ausstiegs-Orders vollständig gefüllt sind. Nach dem Erreichen eines flachen Zustands werden alle internen Zähler zurückgesetzt, damit das nächste Signal ein neues Grid startet.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| Initial Volume | 0.01 | Basis-Lot-Größe für den ersten Trade. |
| Take Profit (pips) | 65 | Distanz in Pips vom letzten Füller, die einen vollständigen Ausstieg auslöst. Verwenden Sie 0, um diese Prüfung zu deaktivieren. |
| Step (pips) | 15 | Adverse Bewegung in Pips erforderlich, bevor in die Position gemittelt wird. |
| Bars To Skip | 45 | Mindestanzahl abgeschlossener Kerzen zwischen Averaging-Orders. |
| Increase Factor | 1.7 | Multiplikator für das Trade-Volumen, jedes Mal wenn eine neue Order auf derselben Seite hinzugefügt wird. |
| Max Volume | 6 | Obergrenze für aggregiertes Volumen (vor Normalisierung durch Marktgrenzen). |
| Min Profit | 10 | Gewinnziel zur Schließung des gesamten Grids wenn der Nettogewinn diesen Betrag überschreitet. |
| Candle Type | 1 Stunde | Zeitrahmen für Kerzenabonnement und Signalberechnungen. |

## Implementierungshinweise
- Pip-Größe wird aus `Security.PriceStep` und Dezimalpräzision abgeleitet. Für Instrumente mit 3 oder 5 Dezimalstellen multipliziert der Code den Preisschritt mit 10, um dem MQL-Konzept eines Pips zu entsprechen.
- Unrealisierter Gewinn wird aus Preisdifferenzen approximiert und umfasst keine Swap- oder Provisionsanpassungen, die im ursprünglichen Experten vorhanden waren.
- Zusätzliche Averaging-Trades werden übersprungen, während Ausstiegs-Orders aktiv sind, was den sequentiellen Ausführungsfluss der ursprünglichen MQL-Logik bewahrt.
- Wenn **Step (pips)** null ist, mittelt die Strategie nie; wenn **Take Profit (pips)** null ist, schließt nur die **Min Profit**-Bedingung das Grid.
