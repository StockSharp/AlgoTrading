# Diagramm der Strategie Fractals Minimum Distance
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Fraktal ist eine Kerze, die höher oder tiefer steht als die jeweils zwei Kerzen zu ihren beiden Seiten, und es kann erst benannt werden, sobald diese späteren Kerzen vorliegen. Dieses Diagramm findet beide Arten, merkt sich den Preis des jüngsten Fraktals jeder Farbe und handelt erst, wenn die beiden weit genug voneinander entfernt sind, damit sich ein Handel dazwischen lohnt. Alles wird an abgeschlossenen Kerzen gemessen, sodass ein Niveau in dem Moment feststeht, in dem es gesetzt wird, und danach nie wieder revidiert wird.

![schema](schema.svg)

## Strategieübersicht

- Eine einzige Kerzenserie versorgt das gesamte Diagramm, und sie liefert ausschließlich abgeschlossene Kerzen, sodass kein Niveau, kein Abstand und keine Order jemals aus einem Preis entsteht, den ein späterer Tick noch zurücknehmen könnte.
- Previous value gibt die Kerze von vor einer Kerze zurück, und Highest und Lowest messen das Extrem der fünf Kerzen, die dort enden — ein Fenster, das vollständig in der Vergangenheit liegt.
- Ein zweites Previous value gibt die Kerze von vor drei Kerzen zurück, genau die Mitte dieses Fensters, und zwei Konverter lesen deren Hoch und deren Tief aus.
- Stimmt das Hoch der mittleren Kerze mit dem Höchstwert des Fensters überein, liegt ein oberes Fraktal vor, und der spiegelbildliche Test am Tief kennzeichnet ein unteres. Eine speichernde Variable hält den Preis jedes Fraktals fest, und da ein solcher Speicher einen falschen Vergleich ignoriert, ändert sich der gespeicherte Preis nur auf einer Kerze, die tatsächlich ein Fraktal ausgebildet hat.
- Ein zweites Variablenpaar gibt die beiden gespeicherten Niveaus auf jeder Kerze erneut aus, sodass die Formel, die den Abstand zwischen ihnen misst, und der Vergleich mit dem Mindestabstand auf jeder Kerze ein Ergebnis liefern und nicht nur auf Fraktalkerzen.
- Jede Seite verknüpft ihren eigenen Fraktaltest mit diesem Abstandstest in einer logischen Bedingung; das angenommene Signal schließt zuerst eine Gegenposition und eröffnet erst danach eine neue, beides per Market-Order.
- Position protection folgt jeder Ausführung und bepreist ihren Ausstieg am Schluss jeder abgeschlossenen Kerze, sodass ein Take-Profit oder ein Stop-Loss einmal pro Kerze geprüft wird.
- Die Kerzen, beide Extreme, beide gespeicherten Niveaus und jede Ausführung werden in einem Chartbereich gezeichnet, sodass sich die beiden Niveaus und der Abstand zwischen ihnen direkt aus dem Bild ablesen lassen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Ein unteres Fraktal ist bestätigt — das Tief von vor drei Kerzen ist das tiefste des Fünf-Kerzen-Fensters, das eine Kerze zuvor endet — und der Abstand zwischen dem jüngsten oberen und dem jüngsten unteren Niveau beträgt mindestens den Mindestabstand. Das Diagramm schließt eine offene Short-Position und kauft dann das Ordervolumen per Market-Order.
- **Short-Einstieg**: Ein oberes Fraktal ist bestätigt — das Hoch von vor drei Kerzen ist das höchste desselben Fensters — unter derselben Abstandsbedingung. Das Diagramm schließt eine offene Long-Position und verkauft dann das Ordervolumen per Market-Order.
- **Ausstieg**: Zwei Dinge können einen Trade beenden. Ein Fraktal der entgegengesetzten Farbe schließt das Offene, bevor der neue Einstieg gesendet wird, weshalb auf jedem Signal ein schließender Block vor dem eröffnenden steht. Unabhängig davon schließt Position protection die Position bei einem Take-Profit oder einem Stop-Loss, gemessen in Prozent des Ausführungspreises und geprüft gegen den Schluss jeder abgeschlossenen Kerze.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der einzigen Kerzenserie. Es werden nur abgeschlossene Kerzen geliefert, sodass einmal pro Kerze entschieden wird. |
| Upper Fractal Length | 5 | Anzahl der Kerzen im Fenster, an dessen höchstem Hoch die mittlere Kerze gemessen wird. |
| Lower Fractal Length | 5 | Anzahl der Kerzen im Fenster, an dessen tiefstem Tief die mittlere Kerze gemessen wird; gleich dem oberen Wert halten, damit das Fraktal symmetrisch bleibt. |
| Fractal Shift | 3 | Wie viele Kerzen zurück die mittlere Kerze liegt. Bei einem Fenster von fünf Kerzen, das um eine Kerze verzögert ist, setzt drei die mittlere Kerze genau in dessen Mitte. |
| Minimum Distance | 100 | Kleinster Abstand zwischen dem jüngsten oberen und dem jüngsten unteren Niveau, der einen Einstieg noch zulässt. Es ist ein absoluter Abstand in den Preiseinheiten des Instruments und muss deshalb jedes Mal neu skaliert werden, wenn das Diagramm auf ein Instrument mit einer anderen Größenordnung der Notierung übertragen wird. |
| Order Volume | 1 | Ordergröße, in Lots. |
| Take Profit, % | 2 | Take-Profit-Abstand, in Prozent des Ausführungspreises, geprüft am Schluss jeder abgeschlossenen Kerze. |
| Stop Loss, % | 1 | Stop-Loss-Abstand, in Prozent des Ausführungspreises, geprüft am Schluss jeder abgeschlossenen Kerze. |

## Diagrammdetails

- Der Indikatorblock stempelt alles, was ihn erreicht, als fertigen Wert ab — er kann eine entstehende Kerze nicht von einer geschlossenen unterscheiden. Dass ausschließlich abgeschlossene Kerzen geliefert werden, hält halbfertige Hochs und Tiefs aus dem Fenster heraus, wo sie das Extrem unbemerkt verschieben und beim nächsten Update wieder überschrieben würden.
- Dasselbe Abonnement hält auch die Orders regelkonform: Eine Order, die aus dem Update einer unfertigen Kerze entsteht, trägt die Eröffnungszeit dieser Kerze und wird als aus der Vergangenheit eintreffend abgelehnt.
- Das Fenster ist um eine Kerze verzögert, damit die beurteilte Kerze genau in seiner Mitte liegt, mit zwei Kerzen davor und zwei danach. Ein Fraktal wird deshalb nie früher als drei Kerzen nach seinem Auftreten beansprucht und danach nie wieder revidiert.
- Der Halte-Trigger verwirft einen falschen Vergleich, statt einen Wert zu speichern, und macht damit aus „der Test war auf dieser Kerze erfolgreich“ ein „das ist der letzte Preis, zu dem er erfolgreich war“. Keines der beiden Niveaus existiert vor seinem ersten Fraktal, sodass vor dem Auftreten beider Farben kein Einstieg möglich ist.
- Beide Einstiegsblöcke handeln per Market-Order und verlangen ausdrücklich nicht, dass die Strategie online ist, sodass sich das Diagramm auf aufgezeichneter Historie genauso verhält wie im Live-Handel.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
