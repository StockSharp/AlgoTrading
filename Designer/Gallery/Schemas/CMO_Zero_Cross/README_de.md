# Diagramm der CMO-Nulllinienkreuzung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Chande Momentum Oscillator schwingt zwischen -100 und +100 und wechselt das Vorzeichen genau dann, wenn Kauf- und Verkaufsdruck die Rollen tauschen. Dieses Diagramm handelt diesen Vorzeichenwechsel, aber nur wenn der neue Wert bereits weit genug von der Null entfernt ist, um eine Order zu rechtfertigen; das flache Pendeln um die Nulllinie wird ignoriert.

![schema](schema.svg)

## Strategieübersicht

- Der Chande Momentum Oscillator wird auf abgeschlossenen Stundenkerzen eines einzelnen Instruments berechnet.
- Die Kreuzung wird aus zwei Werten gelesen, dem Oszillator eine Kerze zuvor und dem aktuellen Wert, statt aus einem Kreuzungsbaustein; damit ist die Richtung der Bewegung im Bild ausdrücklich sichtbar.
- Ein Stärkefilter verlangt, dass sich der neue Wert mindestens um den Mindestabstand von der Null entfernt, und wirft die flachen Kreuzungen weg, die entstehen, während der Markt auf der Stelle tritt.
- Die Position geht in jede Entscheidung ein und bestimmt zugleich die Ordergröße, sodass ein Signal gegen eine offene Position sie mit einer einzigen Marktorder dreht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Oszillator lag auf der vorigen Kerze unter null und steht nun auf oder über der positiven Mindestmarke, und die Position ist nicht long. Die Order kauft das gemeinsame Volumen plus die Größe eines offenen Shorts, sodass eine einzige Marktorder den Short schließt und den Long eröffnet.
- **Short-Einstieg**: Der Oszillator lag auf der vorigen Kerze auf oder über null und steht nun auf oder unter der negativen Mindestmarke, und die Position ist nicht short. Die Order verkauft das gemeinsame Volumen plus die Größe eines offenen Longs.
- **Ausstieg**: Es gibt keinen eigenen Ausstiegsbaustein: Die Position wird entweder durch die Gegenkreuzung der Null gedreht oder vom Positionsschutz geschlossen. Das Original arbeitet mit einem absoluten Take Profit von 2000 und einem Stop Loss von 1000 Kursschritten; absolute Marken, die für ein anderes Instrument abgestimmt wurden, würden auf dieser Historie nie erreicht, deshalb stehen sie hier als zwei Prozent Ziel und ein Prozent Stopp, womit das Verhältnis zwei zu eins erhalten bleibt. Das Original pausiert außerdem vier Kerzen nach jeder Positionsänderung; einen Baustein, der einen Balkenzähler über Kerzen hinweg hält, gibt es nicht, daher entfällt die Pause und die Positionsprüfung allein verhindert einen zweiten Einstieg in dieselbe Richtung.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| CMO Length | 14 | Glättungsperiode des Chande Momentum Oscillator. |
| Min |CMO| | 5 | Mindestabstand zur Null, den der Oszillator erreichen muss, damit die Kreuzung zählt. |
| Volume | 1 | Ordervolumen in Lots. |
| Take profit, % | 2 | Abstand des Take Profit vom Einstiegskurs, in Prozent. |
| Stop loss, % | 1 | Abstand des Stop Loss vom Einstiegskurs, in Prozent. |
| Candles | 01:00:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikatorbaustein mit dem Chande Momentum Oscillator sowie einen Konverter, der den Schlusskurs für den Schutzbaustein liefert.
- Ein Baustein für den vorherigen Wert hält den Oszillator einer Kerze zuvor, und zwei Vergleichsbausteine entscheiden, auf welcher Seite der Null er stand.
- Die Stärkekonstante geht direkt in den Long-Vergleich und über eine kleine negierende Formel in den Short-Vergleich, sodass ein Parameter beide Seiten steuert.
- Jedes logische UND verbindet die vorherige Seite, den Stärkefilter und die Positionsprüfung und löst einen Baustein zur Positionsänderung aus, dessen Volumen aus der Formel stammt, die den Betrag der Position zum gemeinsamen Volumen addiert.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
