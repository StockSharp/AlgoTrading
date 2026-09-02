# Diagramm der Strategie aus MACD und mittlerem Bollinger-Band
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei sehr gebräuchliche Indikatoren teilen sich die Arbeit: Der MACD bestimmt die Marktseite, und das mittlere Bollinger-Band zeigt, wann sich der Kurs weit genug vom fairen Wert entfernt hat, um diese Seite günstig einzunehmen. Die äußeren Bänder bleiben bewusst ungenutzt — die Vorlage kauft Rücksetzer unter der Mittellinie, keine Kanalausbrüche.

![schema](schema.svg)

## Strategieübersicht

- Einziger Trendfilter ist die MACD-Linie gegenüber ihrer Signallinie: darüber nur long, gleichauf oder darunter nur short.
- Der Einstiegskurs muss ein Zehntel Prozent vom mittleren Band entfernt liegen, und zwar auf der dem Trend entgegengesetzten Seite: im Aufwärtstrend werden Rücksetzer gekauft, im Abwärtstrend Ausreißer verkauft.
- Der Abstand ist als Anteil des Bandwertes angegeben und nicht in festen Punkten, deshalb passt dasselbe Diagramm auf jedes Instrument.
- Der Ausstieg wartet gar nicht auf den Kurs: Sobald die beiden MACD-Linien die Plätze tauschen, wird die Position geschlossen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die MACD-Linie liegt über ihrer Signallinie, die Kerze schließt unter dem mittleren Band abzüglich des Abstands, und die Position ist nicht long. Die Order kauft ein Lot: aus der Neutralstellung ein Long, aus einem Short dessen Deckung.
- **Short-Einstieg**: Die MACD-Linie liegt auf oder unter ihrer Signallinie, die Kerze schließt über dem mittleren Band zuzüglich des Abstands, und die Position ist nicht short. Die Order verkauft ein Lot: aus der Neutralstellung ein Short, aus einem Long dessen Schließung.
- **Ausstieg**: Ein Long wird geschlossen, sobald die MACD-Linie auf oder unter ihre Signallinie fällt, ein Short, sobald sie darüber steigt; beide Bausteine stehen im Schließmodus und handeln nur, wenn eine Position besteht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| MACD fast period | 12 | Länge des schnellen Durchschnitts im MACD. |
| MACD slow period | 26 | Länge des langsamen Durchschnitts im MACD. |
| MACD signal period | 9 | Länge der MACD-Signallinie. |
| Bollinger period | 20 | Glättungsperiode der BollingerBands; gelesen wird nur deren Mittellinie. |
| Bollinger width | 2.0 | Standardabweichungs-Multiplikator der BollingerBands; er beeinflusst die Regeln nicht, da die äußeren Bänder ungenutzt bleiben. |
| Middle band gap | 0.001 | Abstand, den der Einstiegskurs zum mittleren Band erreichen muss, als Anteil von dessen Wert. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Ein Kerzenbaustein speist MACD, BollingerBands und einen Konverter für den Schlusskurs; drei weitere Konverter holen MACD-Linie, Signallinie und Mittelband aus den Indikatorwerten.
- Eine einzige Abstandskonstante und zwei Formelbausteine machen aus dem Mittelband eine Kauf- und eine Verkaufsmarke, sodass ein herausgehobener Parameter beide Schwellen zugleich verschiebt.
- Jeder Einstieg ist ein logisches UND aus drei Signalen: dem MACD-Vergleich, dem Bandvergleich und der gegen eine Nullkonstante geprüften Position.
- Die beiden Ausstiegsbausteine hängen direkt an den MACD-Vergleichen und laufen im Schließmodus; alle vier Orderbausteine beziehen ihre Größe aus derselben Volumenkonstante.
- Bewusste Vereinfachungen: Das Original abonniert zusätzlich einen AverageTrueRange, den es nie verwendet, deshalb ist kein ATR-Baustein gezeichnet; außerdem sperrt es Einstiege nach einem Trade für 100 Balken, was kein Baustein abbilden kann — dieses Diagramm steigt wieder ein, sobald die Bedingungen zurückkehren.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
