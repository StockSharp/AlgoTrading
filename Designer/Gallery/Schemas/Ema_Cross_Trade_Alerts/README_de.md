# Strategiediagramm für EMA-Kreuzungen mit Ausführungsmeldungen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt bei Aufwärts- und Abwärtskreuzungen eines schnellen EMA mit Periode 120 und eines langsamen EMA mit Periode 450 auf abgeschlossenen Ein-Minuten-Kerzen. Eine Positionsmomentaufnahme filtert jedes Signal, Market-Orders mit fester Menge steuern das Engagement, jede Ausführung der Strategie wird protokolliert und der Chart zeigt Kerzen, beide EMA-Linien sowie Kauf- und Verkaufsausführungen.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Ein-Minuten-Kerzen speisen den schnellen EMA 120 und den langsamen EMA 450. Der Filter für ausschließlich fertig gebildete Werte ist bei beiden Indikatoren deaktiviert, sodass ihre Werte vom Beginn der Berechnung an verfügbar sind.
- Der Crossing-Baustein gibt `true` aus, wenn der schnelle EMA den langsamen nach oben kreuzt. Ein NOT-Baustein wandelt das `false`-Ereignis der Abwärtskreuzung in ein Verkaufssignal um.
- Bei der Auswertung jeder Kerze gibt eine von der Kerze ausgelöste Positionsmomentaufnahme die aktuelle Position aus, bevor die EMA-Signale verarbeitet werden. Vergleiche erlauben Käufe nur bei `Position <= 0` und Verkäufe nur bei `Position >= 0`.
- Beide Market-Order-Bausteine verwenden `NoCondition` und ein festes Volume von 1. Ein Gegensignal kann eine Position verkleinern oder glattstellen und bei einem Positionsbetrag unter Volume die Nulllinie überschreiten, garantiert aber keine vollständige Umkehr.
- Der Baustein Strategy trades sendet jede Ausführung der Strategie mit der vorgegebenen Vorlage für Ausführungsmeldungen an eine Log-Benachrichtigung. Der Chart empfängt abgeschlossene Kerzen, beide EMA-Linien und die Kauf- und Verkaufsausführungsströme.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn der schnelle EMA den langsamen EMA nach oben kreuzt und die Positionsmomentaufnahme der Kerze kleiner oder gleich null ist, sendet das Diagramm eine Kauforder zum Marktpreis mit Volume 1.
- **Short-Einstieg**: Wenn der schnelle EMA den langsamen EMA nach unten kreuzt und die Positionsmomentaufnahme der Kerze größer oder gleich null ist, sendet das Diagramm eine Verkaufsorder zum Marktpreis mit Volume 1.
- **Ausstieg**: Es gibt keinen eigenen Ausstiegs- oder Schutzbaustein. Eine spätere qualifizierte Order mit fester Menge in Gegenrichtung kann die aktuelle Position verkleinern, eine gleich große Position glattstellen oder die Nulllinie überschreiten, wenn der Betrag der Position kleiner als Volume ist; eine vollständige Umkehr ist nicht garantiert.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:01:00 | Ein-Minuten-Zeitrahmen; nur abgeschlossene Kerzen treiben die EMA- und Entscheidungskette an. |
| Fast EMA Period | 120 | Periode des schnellen ExponentialMovingAverage; der Filter für ausschließlich fertig gebildete Werte ist deaktiviert. |
| Slow EMA Period | 450 | Periode des langsamen ExponentialMovingAverage; der Filter für ausschließlich fertig gebildete Werte ist deaktiviert. |
| Volume | 1 | Feste Menge für beide Market-Order-Bausteine mit NoCondition. |

## Diagrammdetails

- Der Baustein [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) gibt ausschließlich abgeschlossene Ein-Minuten-Kerzen aus und löst die Positionsmomentaufnahme aus, bevor er die EMA-Berechnungen speist.
- Zwei [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Bausteine berechnen ExponentialMovingAverage-Werte mit den Perioden 120 und 450. Ihre Option für ausschließlich fertig gebildete Werte ist `false`; beide Ausgaben gehen außerdem an den Chart.
- Die Ausgabe des [Crossing](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)-Bausteins ist bei einer Aufwärtskreuzung `true` und bei einer Abwärtskreuzung `false`. Eine NOT-[Logikbedingung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) macht aus dem Abwärtsereignis einen positiven Verkaufsauslöser; getrennte AND-Bausteine verbinden Richtung und Position.
- Die aktuelle [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html) wird fortlaufend gespeichert und vor dem EMA-Pfad einmal je Kerze ausgegeben. [Vergleich](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Bausteine prüfen `Position <= 0` und `Position >= 0` im selben Verarbeitungslauf der Kerze wie die Kreuzung.
- Die [Positionsänderung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Bausteine für Kauf und Verkauf platzieren Market-Orders mit `NoCondition` und dem gemeinsamen festen Volume-Wert. Stop-Loss, Gewinnziel und eigener Ausstiegsbaustein fehlen.
- [Strategietrades](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) gibt jeden `MyTrade` der Strategie aus. Der [String Formatter](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) verwendet exakt `EMA cross fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}`.
- Der Baustein [Benachrichtigung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) protokolliert jede formatierte Ausführung mit Type `Log` und Caption `EMA cross trade`. Der Chart zeichnet Kerzen, schnellen EMA, langsamen EMA sowie getrennte Kauf- und Verkaufsausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
