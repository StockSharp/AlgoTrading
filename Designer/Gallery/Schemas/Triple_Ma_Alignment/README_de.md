# Diagramm der Strategie mit drei ausgerichteten EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Drei ExponentialMovingAverage-Bausteine sehr unterschiedlicher Länge laufen auf denselben Kerzen, und ihre Reihenfolge gilt als Trend. Liegt die kurze über der mittleren und die mittlere über der langen, steigt der Markt; ist die Reihenfolge umgekehrt, fällt er. Die Strategie ist immer im Markt und dreht mit einer einzigen Order.

![schema](schema.svg)

## Strategieübersicht

- Es wird nur der Kurs verwendet: kein Oszillator, kein Volatilitätsfilter, allein die Lage dreier exponentieller Durchschnitte zueinander.
- Der bullische Zustand ist kurz über mittel und mittel über lang; der bärische ist kurz höchstens gleich mittel und mittel höchstens gleich lang. Dazwischen, wenn die Linien verschlungen sind, passiert nichts.
- Die aktuelle Position steuert jeden Einstieg, deshalb erzeugt eine Ausrichtung, die Hunderte Kerzen anhält, genau eine Order.
- Einen eigenen Ausstieg gibt es nicht: Die Ordergröße ist Volumen plus Betrag der Position, sodass eine Order die alte Seite schließt und die neue eröffnet.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die kurze ExponentialMovingAverage liegt über der mittleren, die mittlere über der langen, und die Position ist noch nicht long. Die Order kauft Volumen plus Positionsbetrag: aus der Neutralstellung ein Long, aus einem Short der Dreh in einen Long.
- **Short-Einstieg**: Die kurze ExponentialMovingAverage liegt höchstens auf der mittleren, die mittlere höchstens auf der langen, und die Position ist noch nicht short. Die Order verkauft Volumen plus Positionsbetrag: aus der Neutralstellung ein Short, aus einem Long der Dreh in einen Short.
- **Ausstieg**: Es gibt keinen eigenen Ausstiegsbaustein. Eine Position wird nur bei der gegenteiligen Ausrichtung verlassen, und durch die Drehgröße steht das Diagramm keine einzige Kerze lang an der Seitenlinie.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Short EMA period | 100 | Länge der schnellsten ExponentialMovingAverage. |
| Middle EMA period | 250 | Länge der mittleren ExponentialMovingAverage. |
| Long EMA period | 500 | Länge der langsamsten ExponentialMovingAverage. |
| Volume | 1 | Basisvolumen der Order in Lots; beim Drehen kommt der Positionsbetrag hinzu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Ein Kerzenbaustein versorgt alle drei Indikatorbausteine, sodass die Durchschnitte stets auf denselben abgeschlossenen Kerzen gerechnet werden.
- Vier Vergleichsbausteine bilden die beiden Zustände: zwei strenge Größer-Vergleiche für den bullischen Stapel, zwei Kleiner-gleich-Vergleiche für den bärischen — genau die Negation aus dem Originalcode.
- Jedes logische UND verbindet die beiden Durchschnittsvergleiche mit der gegen eine Nullkonstante geprüften Position und löst einen Baustein zur Positionsänderung aus.
- Ein Formelbaustein addiert den Positionsbetrag zur Volumenkonstante und speist beide Orderbausteine — dadurch wird aus einem Einstieg ein Dreh.
- Bewusste Vereinfachungen: Das Original läuft auf Minutenkerzen, dieses Diagramm auf Fünf-Minuten-Kerzen, dieselben Längen decken also den fünffachen Zeitraum ab. Das Original merkt sich zudem, ob die Ausrichtung schon auf der Vorkerze bestand; dieses Merkmal entfällt, weil die Positionsprüfung einen Wiedereinstieg ebenso zuverlässig verhindert. Der deklarierte Zwei-Prozent-Stopp wird im Code nie gesetzt, deshalb ist kein Schutzbaustein gezeichnet.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
