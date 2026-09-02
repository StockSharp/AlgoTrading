# Diagramm der Strategie Color Schaff Trend Cycle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Schaff Trend Cycle ist ein Stochastik-Wert über dem MACD-Histogramm; er reagiert daher schneller als ein gewöhnlicher Oszillator und bewegt sich trotzdem im Band von null bis hundert. Das Diagramm handelt den Moment, in dem der Zyklus die Mitte dieses Bandes verlässt, und lässt eine schlichte MACD-Linie entscheiden, ob der Ausbruch es wert ist: Nur Ausbrüche nach oben bei positivem MACD und nach unten bei negativem MACD werden zu Orders.

![schema](schema.svg)

## Strategieübersicht

- Der Schaff Trend Cycle läuft über abgeschlossene Kerzen, und ein Vorwert-Baustein hält seinen Stand eine Kerze zuvor, damit ein Durchbruch von einem bloßen Verweilen über der Marke unterschieden werden kann.
- Zwei Marken rahmen die Mitte des Bandes ein: der Durchbruch der oberen von unten ist das Long-Signal, der Durchbruch der unteren von oben das Short-Signal.
- Die MACD-Linie, die Differenz aus einem schnellen und einem langsamen exponentiellen Durchschnitt, dient nur als Vorzeichenfilter: positiv erlaubt Longs, negativ Shorts.
- Nach dem ersten Trade ist die Strategie stets im Markt: Jedes Signal dreht die Position, denn das Ordervolumen ist das Basisvolumen zuzüglich des bereits gehaltenen Bestands.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Auf der Vorkerze lag der Zyklus auf oder unter der oberen Marke und liegt nun darüber, die MACD-Linie ist positiv und die Position ist nicht long. Die Order kauft das Basisvolumen zuzüglich des Positionsbetrags: Ein Short dreht auf Long, aus der Neutralstellung entsteht ein Long.
- **Short-Einstieg**: Auf der Vorkerze lag der Zyklus auf oder über der unteren Marke und liegt nun darunter, die MACD-Linie ist negativ und die Position ist nicht short. Die Order verkauft das Basisvolumen zuzüglich des Positionsbetrags: Ein Long dreht auf Short, aus der Neutralstellung entsteht ein Short.
- **Ausstieg**: Einen eigenen Ausstieg oder Schutzorders gibt es nicht, genau wie im Original: Die Position wird nur verlassen, wenn der gegenläufige Marken-Durchbruch kommt und sie dreht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| STC smoothing length | 10 | Glättungsperiode des Schaff Trend Cycle; größere Werte machen den Zyklus träger und die Durchbrüche seltener. |
| MACD fast EMA | 12 | Schneller exponentieller Durchschnitt im MACD-Filter. |
| MACD slow EMA | 26 | Langsamer exponentieller Durchschnitt im MACD-Filter. |
| Upper level | 60 | Marke, die der Zyklus für ein Long-Signal nach oben durchbrechen muss. |
| Lower level | 40 | Marke, die der Zyklus für ein Short-Signal nach unten durchbrechen muss. |
| Volume | 1 | Basisordervolumen in Lots; beim Drehen kommt der Betrag der Position hinzu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Schaff Trend Cycle und den MACD; ein Vorwert-Baustein liest den Zyklus eine Kerze zurück.
- Vier Vergleichsbausteine bilden die beiden Durchbrüche: der Vorwert gegen eine Marke und der aktuelle Wert gegen dieselbe Marke, was zusammen heißt, dass die Linie sie auf dieser Kerze überschritten hat.
- Zwei weitere Vergleiche liefern das Vorzeichen der MACD-Linie, und zwei stellen die Position der gemeinsamen Nullkonstante gegenüber, damit ein Signal eine bestehende Position nicht vergrößert.
- Jedes logische UND verbindet vier Bedingungen - wo der Zyklus war, wo er ist, das MACD-Vorzeichen und die Position - und löst einen Baustein zur Positionsänderung aus.
- Ein Formelbaustein berechnet die Drehgröße als Basisvolumen plus Positionsbetrag, sodass eine Marktorder die alte Seite schließt und die neue eröffnet - genau wie das Orderpaar im C#-Code.
- Zwei Abweichungen vom C#-Original sind erwähnenswert. Das Original trägt den Namen des Schaff Trend Cycle, berechnet an seiner Stelle aber tatsächlich einen RSI über zehn Perioden; dieses Diagramm verwendet den echten Schaff-Trend-Cycle-Indikator, die Signale entsprechen also dem Namen und nicht dem Code.
- Zudem arbeitet das Original mit Vier-Stunden-Kerzen, von denen der mitgelieferte Monat Historie viel zu wenige enthält; das Diagramm läuft auf Fünf-Minuten-Kerzen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
