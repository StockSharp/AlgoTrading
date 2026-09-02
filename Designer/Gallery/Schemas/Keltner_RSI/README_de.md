# Diagramm der Strategie Keltner RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Mean-Reversion-Diagramm rund um die Mittellinie eines Keltner-Kanals. Ein Kurs, der unter die EMA gelaufen ist, wird bei schwachem RSI gekauft; ein Kurs über der EMA wird bei starkem RSI verkauft, und der Trade wird abgegeben, sobald der Kurs die Linie wieder kreuzt und der RSI seine Mitte überschritten hat. Die Originalstrategie berechnet die ATR-Bänder des Kanals, liest sie aber nie aus, deshalb lässt dieses Diagramm sie weg und behält nur das, was tatsächlich über einen Trade entscheidet.

![schema](schema.svg)

## Strategieübersicht

- Die ExponentialMovingAverage über 20 Perioden ist die Mittellinie des Keltner-Kanals und die einzige Kursreferenz des gesamten Diagramms.
- Der RSI über 14 Kerzen liefert die zweite Meinung: ein Wert unter 45 bestätigt den Abverkauf, der gekauft wird, ein Wert über 55 den Schub, der verkauft wird.
- Beide Einstiege verlangen eine neutrale Position, beide Ausstiege sind schließende Bausteine, sodass sich die vier Zweige nie um dieselbe Position streiten.
- Zwei Vereinfachungen gegenüber dem Original: die ungenutzten ATR-Bänder entfallen, und die Pause von 120 Balken nach jeder Ausführung hat keinen Zählerbaustein, weshalb dieses Diagramm häufiger handelt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt unter der EMA, der RSI unter der Long-Einstiegsmarke und die Position ist neutral. Die Order kauft das gemeinsame Volumen zu Markt und eröffnet den Long.
- **Short-Einstieg**: Der Schlusskurs liegt über der EMA, der RSI über der Short-Einstiegsmarke und die Position ist neutral. Die Order verkauft das gemeinsame Volumen zu Markt und eröffnet den Short.
- **Ausstieg**: Ein Long wird geschlossen, wenn der Schlusskurs wieder über der EMA liegt und der RSI über seiner Mitte steht; ein Short, wenn der Schlusskurs wieder unter der EMA liegt und der RSI unter der Mitte. Stop-Loss und Take-Profit fehlen, genau wie im Originalcode, in dem der deklarierte Stop-Prozentsatz nie angewendet wird.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| EMA Length | 20 | Periode der ExponentialMovingAverage, die als Mittellinie des Kanals dient. |
| RSI Length | 14 | Glättungsperiode des RelativeStrengthIndex. |
| RSI Long Entry | 45 | Der RSI muss für einen Long-Einstieg unter dieser Marke liegen. |
| RSI Short Entry | 55 | Der RSI muss für einen Short-Einstieg über dieser Marke liegen. |
| RSI Exit Level | 50 | Mittelwert, den der RSI zum Schließen einer Position überschreiten muss. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist die EMA, den RSI und einen Konverter, der den Schlusskurs liest.
- Zwei Vergleichsbausteine stellen den Schlusskurs der EMA gegenüber, vier weitere prüfen den RSI gegen seine drei Marken, und der Positionsbaustein wird mit einer Nullkonstante verglichen.
- Zwei logische UND bauen die Einstiege aus einer Kursbedingung, einer RSI-Bedingung und der Prüfung auf neutrale Position und steuern Positionsbausteine im Eröffnungsmodus.
- Zwei weitere logische UND bauen die Ausstiege und steuern Positionsbausteine im Schließmodus; sie brauchen kein Volumen und wirken nur auf die Seite, die sie schließen können.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
