# Strategiediagramm für drei rote Kerzen mit zeitgesteuertem Ausstieg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt drei aufeinanderfolgende Kerzen derselben Farbe bei erhöhter Volatilität. Drei rote Kerzen mit ATR 14 über dem 0,8-Fachen seines Mittelwerts aus 30 Werten erzeugen ein Long-Setup; drei grüne Kerzen erzeugen unter derselben Volatilitätsregel ein Short-Setup. Bei neutraler Position erfolgt der Einstieg direkt, eine Gegenposition wird über eine bestätigte Zweischrittfolge gedreht, und eine offene Position kann beim entgegengesetzten Drei-Kerzen-Muster oder nach zwanzig abgeschlossenen Balken geschlossen werden. Jede Aktionsfolge startet eine Sperre von zwölf Kerzen.

![schema](schema.svg)

## Strategieübersicht

- Verarbeitet werden nur abgeschlossene 30-Minuten-Kerzen. Zwei Previous-value- und sechs Converter-Blöcke liefern Open und Close der aktuellen sowie der zwei vorherigen Kerzen, sodass jeder Balken ein gleitendes Drei-Kerzen-Fenster für Rot und Grün prüft.
- ATR 14 und sein einfacher Mittelwert aus 30 Werten müssen beide gebildet sein. Hohe Volatilität ist die strenge Bedingung `ATR > ATR-Mittelwert × 0,8`; während der Aufwärmphase gibt es keine Handelsentscheidung.
- Drei qualifizierte rote Kerzen kaufen aus neutraler Position oder drehen einen Short. Drei qualifizierte grüne Kerzen verkaufen aus neutraler Position oder drehen einen Long. Beim Drehen wird zuerst eine Einheit per ReduceOnly geschlossen; erst der vollständig ausgeführte Schließungs-Order eröffnet eine Einheit in Gegenrichtung.
- Ohne qualifizierte Hochvolatilitätsumkehr schließen drei grüne Kerzen einen Long und drei rote einen Short. Ein zustandsbehafteter Zähler schließt außerdem nach zwanzig abgeschlossenen Haltebalken; eine Umkehr auf demselben Balken hat Vorrang vor dem Zeitausstieg.
- Drei Combination-Blöcke führen die zwei Long-Ausstiegsgründe, die zwei Short-Ausstiegsgründe und die Ausführungsströme aller acht Aktionen zusammen. Eine Ausführung sperrt die nächsten zwölf abgeschlossenen Kerzen; die dreizehnte ist die erste wieder zulässige. Stop-Loss, Take-Profit und Positionsschutzblock sind nicht vorhanden.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn die aktuelle und die zwei vorherigen abgeschlossenen Kerzen unter ihrem Open schließen, ATR 14 streng über `ATR-Mittelwert × 0,8` liegt und die Sperre bereit ist, sendet eine neutrale Position einen NoCondition-Market-Kauf mit Order Volume 1. Aus einem Short wird zuerst ein ReduceOnly-Market-Kauf über 1 gesendet; sein vollständig ausgeführter Order aktualisiert den Volumenwert und löst den NoCondition-Market-Kauf über 1 aus.
- **Short-Einstieg**: Wenn die aktuelle und die zwei vorherigen abgeschlossenen Kerzen über ihrem Open schließen, ATR 14 streng über `ATR-Mittelwert × 0,8` liegt und die Sperre bereit ist, sendet eine neutrale Position einen NoCondition-Market-Verkauf mit Order Volume 1. Aus einem Long wird zuerst ein ReduceOnly-Market-Verkauf über 1 gesendet; sein vollständig ausgeführter Order aktualisiert den Volumenwert und löst den NoCondition-Market-Verkauf über 1 aus.
- **Ausstieg**: Ein Long wird bei drei aufeinanderfolgenden grünen Kerzen geschlossen, sofern das Muster nicht zugleich eine Hochvolatilitätsumkehr bildet, oder wenn Max Hold Bars 20 erreicht. Ein Short wird spiegelbildlich bei drei roten Kerzen oder nach 20 Balken geschlossen. Muster- und Timerereignisse werden je Seite zusammengeführt, und ein Flag erlaubt höchstens eine eigenständige Schließung pro Kerze. Jede solche Schließung und beide Ausführungen einer gestuften Umkehr speisen den gemeinsamen Sperrstrom.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles Series | 00:30:00 | 30-Minuten-Serie; nur abgeschlossene Kerzen aktualisieren Muster, Indikatoren, Zähler, Sperre und Entscheidungen. |
| ATR Length | 14 | Periode des Average-True-Range-Indikators, der nur gebildete Werte ausgibt. |
| ATR Average Length | 30 | Periode des einfachen gleitenden Mittelwerts der ATR-Werte; Entscheidungen warten, bis dieser Mittelwert gebildet ist. |
| ATR Multiplier | 0.8 | Multiplikator des ATR-Mittelwerts. Volatilität gilt nur, wenn ATR den resultierenden Schwellenwert streng überschreitet. |
| Max Hold Bars | 20 | Anzahl abgeschlossener Balken, die bei offener Position bis zur Freigabe der zeitgesteuerten Schließung gezählt werden. |
| Cooldown Bars | 12 | Anzahl nachfolgender abgeschlossener Kerzen, die nach einer Aktionsfolge gesperrt sind; auf Kerze 13 beginnen Entscheidungen erneut. |
| Order Volume | 1 | Feste Menge für Einstiege aus neutraler Position, ReduceOnly-Schließungen und Einstiege nach ausgeführter Schließung; das Diagramm ist für seine eigene Einheitsposition ausgelegt. |

## Diagrammdetails

- Der [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Block gibt abgeschlossene 30-Minuten-Kerzen aus. Zwei [Previous value](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)-Blöcke halten die Verschiebungen 1 und 2, sechs [Converter](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/converter.html)-Blöcke extrahieren die drei Open/Close-Paare.
- Sechs [Comparison](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Blöcke klassifizieren jede Kerze streng mit `Close < Open` und `Close > Open`. Zwei [Logical condition](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)-Blöcke mit je drei Eingängen bilden die gleitenden roten und grünen Muster; ein Doji macht beide falsch.
- Zwei [Indicator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Blöcke für gebildete Werte berechnen ATR 14 und SMA 30 des ATR. Ein [Formula](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html)-Block multipliziert den Mittelwert mit 0,8; ein strenger Vergleich liefert das Hochvolatilitätssignal.
- Die aktuelle [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html) wird pro Kerze zweimal erfasst: für die Handelsroute und für den Haltezähler. [Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)- und Formula-Blöcke erhöhen den Zähler nur bei offener Position und setzen ihn durch bestätigte Aktionsausführungen zurück.
- Zwei boolesche [Combination](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/combination.html)-Blöcke führen Muster- und Timerausstiege unverändert zusammen. Seitenspezifische [Flag](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/flag.html)-Blöcke verhindern doppelte eigenständige Schließungen; Prioritätstore unterdrücken sie, wenn dieselbe Kerze bereits eine Umkehr erfüllt.
- Acht [Modify position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Blöcke implementieren zwei Einstiege aus neutraler Position, zwei eigenständige Ausstiege und zwei gestufte Umkehrungen. Jede Umkehr nutzt `ReduceOnly close 1 → fully matched Order → NoCondition open 1`; der ausgeführte Order gibt im selben Ereigniszyklus auch das Volumen erneut aus.
- Eine MyTrade-Combination sendet jede Aktionsausführung an die [N values](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)-Sperre. Die erste Ausführung startet zwölf Kerzen, während der zweite Teil derselben Umkehr den aktiven Zähler nicht neu startet. Das [Chart panel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/chart.html) erhält Kerzen, ATR, Mittelwert, Schwelle und alle Aktionsausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
