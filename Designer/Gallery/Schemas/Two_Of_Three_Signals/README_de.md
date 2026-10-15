# Strategiediagramm für Zwei-von-drei-Richtungssignale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm bewertet auf jeder abgeschlossenen 30-Minuten-Kerze drei Richtungsstimmen: die Steigung der MACD-Signallinie, den Stochastic-%K-Bereich und den RSI-Bereich. Jedes übereinstimmende Paar erzeugt für diese Kerze genau ein Mehrheitsereignis. Ist der Cooldown bereit, wird aus einer neutralen Position eine Position mit Volumen 1 eröffnet; eine entgegengesetzte Position wird dagegen durch einen ReduceOnly-Schluss und einen ausführungsbestätigten Einstieg in die neue Richtung umgekehrt. Jede Ausführung einer neuen Position startet einen Cooldown von zehn Kerzen.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene 30-Minuten-Kerzen versorgen die MACD-12/26/9-, Stochastic-14/3- und RSI-14-Indikatoren, die erst nach vollständiger Bildung ausgeben. Ein Previous value-Baustein hält den vorherigen Wert der MACD-Signallinie fest, und ein Historien-Gate verhindert eine Entscheidung, bis dieser Wert vorhanden ist.
- Die Long-Stimmen lauten `MACD Signal > Previous MACD Signal`, `Stochastic %K ≤ 20` und `RSI < 40`. Die Short-Stimmen lauten `MACD Signal < Previous MACD Signal`, `Stochastic %K ≥ 80` und `RSI > 60`. Gleiche MACD-Werte und Oszillatorwerte außerhalb der Richtungsbereiche sind neutral.
- Drei paarweise Logical condition-Bausteine bilden für jede Richtung alle möglichen Mehrheiten aus zwei Stimmen ab. Ein Flag je Kerze lässt nur das erste erfüllte Paar passieren, sodass auch drei übereinstimmende Indikatoren nur ein Richtungsereignis statt drei erzeugen.
- Positions- und Cooldown-Snapshots leiten jede Mehrheit weiter. Bei bereitem Cooldown sendet eine neutrale Position einen NoCondition-Markteinstieg mit Order Volume 1. Bei einer entgegengesetzten Position wird zuerst ein ReduceOnly-Marktauftrag zum Schließen von 1 gesendet; erst die vollständig ausgeführte Close-Order startet den NoCondition-Markteinstieg mit 1 in die neue Richtung.
- Ein Combination-Baustein führt die Ausführungen der vier Aktionen für neue Positionen zu einem Cooldown-Strom zusammen. Eine Ausführung sperrt das Diagramm für Einstiege, die nächsten zehn abgeschlossenen Kerzen werden übersprungen, und die elfte abgeschlossene Kerze ist die erste wieder entscheidungsberechtigte Kerze. Es gibt weder einen eigenständigen Ausstieg noch Stop-Loss-, Take-Profit- oder Positionsschutz-Bausteine.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn zwei beliebige Long-Stimmen übereinstimmen und der Cooldown bereit ist, sendet eine neutrale Position einen NoCondition-Marktkauf mit Order Volume 1. Ist die Position short, sendet das Diagramm zuerst einen ReduceOnly-Marktkauf mit 1; erst dessen vollständig ausgeführte Close-Order löst den NoCondition-Marktkauf mit 1 aus. Eine bestehende Long-Position bleibt unverändert.
- **Short-Einstieg**: Wenn zwei beliebige Short-Stimmen übereinstimmen und der Cooldown bereit ist, sendet eine neutrale Position einen NoCondition-Marktverkauf mit Order Volume 1. Ist die Position long, sendet das Diagramm zuerst einen ReduceOnly-Marktverkauf mit 1; erst dessen vollständig ausgeführte Close-Order löst den NoCondition-Marktverkauf mit 1 aus. Eine bestehende Short-Position bleibt unverändert.
- **Ausstieg**: Es gibt keine separate Ausstiegsregel. Eine Mehrheit in der entgegengesetzten Richtung schließt zuerst die aktuelle Seite und eröffnet danach in einer gestuften Sequenz die neue Seite. Der ReduceOnly-Schluss kann das Engagement nicht erhöhen, beide Schritte verwenden jedoch das feste Order Volume 1. Die Sequenz ist für die vom Diagramm erzeugte Einheitsposition dimensioniert; bei einer abweichenden tatsächlichen Positionsgröße wird die Position möglicherweise nicht vollständig geschlossen oder endet nicht in der signalisierten Richtung.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles Series | 00:30:00 | 30-Minuten-Kerzenserie; nur abgeschlossene Kerzen aktualisieren die Indikatoren, setzen die Mehrheits-Flags je Kerze zurück, führen den Cooldown weiter und stoßen Entscheidungen an. |
| MACD Fast Length | 12 | Im MACD-Indicator-Block fest eingestellt; zum Ändern der schnellen EMA-Periode diesen Block bearbeiten. |
| MACD Slow Length | 26 | Im MACD-Indicator-Block fest eingestellt; zum Ändern der langsamen EMA-Periode diesen Block bearbeiten. |
| MACD Signal Length | 9 | Im MACD-Indicator-Block fest eingestellt; zum Ändern der Signal-EMA-Periode diesen Block bearbeiten. Ihre Steigung über eine Kerze liefert die MACD-Stimme. |
| Stochastic K Length | 14 | Im Stochastic-Indicator-Block fest eingestellt; zum Ändern der %K-Periode diesen Block bearbeiten. Die Long- und Short-Schwellen liegen bei 20 und 80. |
| Stochastic D Length | 3 | Im Stochastic-Indicator-Block fest eingestellt; zum Ändern der %D-Periode diesen Block bearbeiten. Die Richtungsstimme liest %K, und der vollständige Indikator muss gebildet sein. |
| RSI Length | 14 | RSI-Periode. Die festen Richtungsschwellen liegen strikt unter 40 und strikt über 60. |
| Cooldown Bars | 10 | Anzahl der nach einer Ausführung einer neuen Position gesperrten abgeschlossenen Kerzen; Entscheidungen werden bei Kerze 11 fortgesetzt. |
| Order Volume | 1 | Feste Menge für Einstiege aus einer neutralen Position, ReduceOnly-Schlussaktionen und ausführungsbestätigte Einstiege nach einem Schluss. |

## Diagrammdetails

- Der [Candles](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Baustein gibt nur abgeschlossene 30-Minuten-Kerzen aus. Drei erst nach vollständiger Bildung ausgebende [Indicator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Bausteine berechnen MACD 12/26/9, Stochastic 14/3 und RSI 14.
- Ein [Converter](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/converter.html) extrahiert die MACD-Signallinie. Ein [Previous value](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)-Baustein hält ihren Wert um eine Kerze zurück, und strikte Vergleiche klassifizieren das aktuelle Signal als steigend, fallend oder unverändert.
- Weitere [Comparison](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Bausteine setzen die exakten festen Oszillatorgrenzen um: Stochastic %K verwendet `≤ 20` und `≥ 80`, RSI dagegen `< 40` und `> 60`. Die Schwellen und die erforderlichen zwei Stimmen sind feste Diagrammeinstellungen und keine freigegebenen Parameter.
- Sechs paarweise [Logical condition](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)-Bausteine decken die drei möglichen Long-Paare und die drei möglichen Short-Paare ab. Zwei [Flag](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/flag.html)-Bausteine werden bei jeder Kerze zurückgesetzt und verdichten mehrere erfüllte Paare zu einem Mehrheitsereignis je Richtung.
- Die aktuelle [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html) und der Cooldown-Bereitschaftsstatus werden für den Kerzenzyklus erfasst. Positionsvergleiche unterscheiden neutrales, Long- und Short-Engagement; die Einstiegsbedingungen verlangen sowohl ein Mehrheitsereignis als auch einen verfügbaren Cooldown.
- Sechs [Modify position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Bausteine setzen zwei Einstiege aus neutraler Position und zwei gestufte Umkehrungen um. Die neutralen Wege werden extern durch `Position = 0` abgesichert; die Umkehr-Schlussbausteine verwenden ReduceOnly, und ihre vollständig ausgeführten Order-Ausgänge lösen die festen NoCondition-Einstiege in die Gegenrichtung aus.
- Ein [Combination](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/combination.html)-Baustein führt die MyTrade-Ausgänge der vier Bausteine für neue Positionen sofort zusammen, ohne sie zu zählen oder zu verändern. Die zusammengeführte Ausführung deaktiviert die Einstiegsbereitschaft und löst einen [N values](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)-Baustein aus, der zehn nachfolgende abgeschlossene Kerzen zählt, bevor er die Bereitschaft für Kerze 11 wiederherstellt. Das [Chart panel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/chart.html) empfängt die Kerzen, drei numerische Signalströme, die zusammengeführten Ausführungen neuer Positionen und beide Ausführungen der Umkehr-Schlüsse.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
