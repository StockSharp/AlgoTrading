# Strategiediagramm Overnight Session Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm verbindet einen ausgebildeten SMA mit Periode 20 mit zwei Zeitfenstern für geplante Market-Orders. Die Strategiezeit wertet die letzte abgeschlossene Fünf-Minuten-Kerze, die Position und das Kalenderdatum aus: Eine passende Kauforder kann während Stunde 20 und eine passende Verkaufsorder während Stunde 8 gesendet werden. Ein Datumsspeicher begrenzt das Diagramm auf eine gesendete Order je Kalenderdatum.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen aktualisieren den Schlusskurs und den SimpleMovingAverage mit Periode 20.
- Der SMA-Baustein gibt nur ausgebildete Werte aus; geplante Entscheidungen warten daher auf eine ausreichende Kerzenhistorie.
- Der Time-Baustein dient als Entscheidungstakt. Jeder Takt gibt die zuletzt gespeicherten Werte von Schlusskurs, SMA und Position frei und liefert danach Stunde und Kalenderbestandteile für die Filter.
- Beide Market-Order-Bausteine verwenden das feste Volumen 1 ohne Bedingung zur Positionsänderung. Die Positionsfilter erlauben Käufe nur bei einer Position kleiner oder gleich null und Verkäufe nur bei einer Position größer oder gleich null.
- Ein numerischer Kalenderschlüssel wird vor dem Auslösen des Order-Bausteins gespeichert. Der Chart zeigt Kerzen, SMA-Werte und beide MyTrade-Ströme; Schutz und separater Ausstieg sind nicht vorhanden.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Während Night Hour 20 liegt der Schlusskurs der letzten abgeschlossenen Kerze über dem ausgebildeten SMA, die aktuelle Position ist kleiner oder gleich null und am aktuellen Kalenderdatum wurde noch keine Order gesendet. Das Diagramm sendet einen Market-Kauf mit Volume 1.
- **Short-Einstieg**: Während Day Hour 8 liegt der Schlusskurs der letzten abgeschlossenen Kerze unter dem ausgebildeten SMA, die aktuelle Position ist größer oder gleich null und am aktuellen Kalenderdatum wurde noch keine Order gesendet. Das Diagramm sendet einen Market-Verkauf mit Volume 1.
- **Ausstieg**: Es gibt weder eine eigene Ausstiegsorder noch Schutzorders. Eine spätere passende Order mit fester Größe in Gegenrichtung kann eine Position verkleinern, eine gleich große Gegenposition schließen oder bei einem kleineren aktuellen Betrag als Volume die Nulllinie überschreiten; eine vollständige Umkehr ist nicht gewährleistet.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Fünf-Minuten-Zeitrahmen; nur abgeschlossene Kerzen aktualisieren die gespeicherten Preis- und SMA-Werte. |
| SMA Period | 20 | Anzahl abgeschlossener Kerzen für SimpleMovingAverage; Entscheidungen erfordern einen ausgebildeten SMA-Wert. |
| Night Hour | 20 | Stunde der Strategiezeit, in der die Kaufbedingungen eine Order senden können. |
| Day Hour | 8 | Stunde der Strategiezeit, in der die Verkaufsbedingungen eine Order senden können. |
| Volume | 1 | Feste Menge für beide Market-Order-Bausteine. |

## Diagrammdetails

- Der [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Strom speist einen Schlusskurs-[Konverter](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) und einen [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html), der nur den ausgebildeten SMA ausgibt. Eine [Formel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html) mit dem Ausdruck `a` stellt den SMA als Zahl bereit.
- [Aktuelle Zeit](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) liefert den Zeitstempel der Strategie oder Nachricht. Bei jedem Takt löst sie [Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)-Bausteine mit den letzten Schlusskurs-, SMA- und Positionswerten aus, sodass Time direkt an jeder Entscheidung beteiligt ist.
- Zeitkonverter entnehmen Hour, Year und DayOfYear. Die Datums-[Formel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html) berechnet `Year * 1000 + DayOfYear` und erzeugt einen stabilen Schlüssel für jedes Kalenderdatum.
- [Vergleich](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Bausteine prüfen beide geplanten Stunden, Schlusskurs gegen SMA, Position gegen null und den aktuellen Datumsschlüssel gegen den zuletzt gespeicherten Schlüssel. Zwei Bausteine für [Logische Bedingung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) verbinden die Kauf- und Verkaufsfilter.
- Die aktuelle [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html) wird bei jedem Zeittakt abgetastet. Für den Kauf gilt `Position <= 0`, für den Verkauf `Position >= 0`.
- Ein wahres kombiniertes Signal speichert zuerst den aktuellen Datumsschlüssel und löst danach den zugehörigen Baustein [Position ändern](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) aus. Diese Reihenfolge verhindert eine weitere gesendete Order bei späteren Takten desselben Kalenderdatums.
- Beide Modify-position-Bausteine erhalten den gemeinsamen festen Volume-Wert und platzieren Market-Orders. Das Chart-Panel empfängt abgeschlossene Kerzen, den ausgebildeten SMA-Strom und die MyTrade-Ausgänge der Kauf- und Verkaufsbausteine.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
