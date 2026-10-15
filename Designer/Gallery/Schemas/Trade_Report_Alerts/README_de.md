# Diagramm der Strategie Trade Report Alerts
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm ist eher ein Beispiel für Berichterstattung als für das Erfinden von Signalen. Eine schlichte Kreuzung eines exponentiellen gleitenden Durchschnitts über 9 und eines über 26 Perioden auf abgeschlossenen Fünf-Minuten-Kerzen liefert die Trades, und alles darum herum verwandelt diese Trades in lesbaren Text: Jede eigene Ausführung wird in dem Moment, in dem sie geschieht, zu einer Logzeile, und einmal am Tag schreibt ein uhrzeitgesteuerter Zweig das realisierte Ergebnis der Strategie heraus. Die Berichtsseite liest die Handelsseite und platziert, ändert oder blockiert niemals eine eigene Order.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen speisen einen schnellen ExponentialMovingAverage über 9 und einen langsamen über 26. Der Crossing-Block reduziert das Paar auf ein einziges Ereignis: `true`, wenn die schnelle Linie die langsame nach oben kreuzt, `false`, wenn sie sie nach unten kreuzt, und dazwischen gar nichts.
- Die Position wird einmal je Kerze über eine kerzengetriebene Momentaufnahme gelesen, und drei Vergleiche gegen null beschreiben sie als flat, long oder short. Jede Entscheidung baut auf dieser Momentaufnahme auf, sodass eine mitten in der Kerze eintreffende Ausführung eine bereits getroffene Entscheidung nicht erneut aufreißen kann.
- Aus einer flachen Position heraus eröffnet eine Aufwärtskreuzung eine Long-Position und eine Abwärtskreuzung eine Short-Position. Beide Einstiegsblöcke tragen die Bedingung Open-position, sie bleiben also stumm, solange irgendeine Position gehalten wird, und können nicht Order auf Order türmen.
- Aus einer gehaltenen Position heraus schließt die entgegengesetzte Kreuzung sie über einen Close-position-Block, der die Ordergröße aus der Position selbst ableitet. Das Orderereignis dieser Schließung löst dann den Einstieg in die neue Richtung aus, sodass eine Umkehr als zwei ausdrückliche Schritte geschrieben wird und nicht als eine überdimensionierte Order.
- Ein einziger nach außen gelegter Wert Volume speist alle vier Einstiegsblöcke; die beiden Schließblöcke nehmen kein Volumen entgegen, weil ein Close-position-Block bereits weiß, wie viel offen ist.
- Der Block Strategy trades greift jede eigene Ausführung der Strategie ab und schickt sie durch einen String formatter in eine Log-Benachrichtigung, sodass jede Ausführung eine Zeile mit Richtung, Menge, Instrument und Preis hinterlässt.
- Ein zweiter Zweig berichtet nach der Uhr statt nach dem Markt. Current time speist zwei Working-time-Fenster – ein Berichtsfenster um die Mittagszeit und ein Rücksetzfenster kurz nach Mitternacht – und ein dazwischengesetztes Flag verwandelt das gesamte Berichtsfenster in genau einen Impuls pro Tag.
- Dieser einzelne Impuls gibt das realisierte Ergebnis der Strategie aus einer Variablen frei, die den zuletzt zugewiesenen Wert hält, formatiert es und schreibt es ins Log. Da die Variable bei null beginnt, erscheint eine Statuszeile auch an einem Tag, der überhaupt keine Ausführung hervorgebracht hat.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Eine Aufwärtskreuzung des schnellen exponentiellen Durchschnitts über den langsamen, ausgewertet während die Momentaufnahme zur Kerzenzeit eine flache Position zeigt, sendet einen Market-Kauf über Volume. Ist stattdessen eine Short-Position offen, schließt dieselbe Kreuzung sie zunächst vollständig, und die daraus entstehende Schließorder löst sofort den Long-Einstieg aus, sodass der Richtungswechsel innerhalb derselben Kerze stattfindet.
- **Short-Einstieg**: Eine Abwärtskreuzung des schnellen exponentiellen Durchschnitts unter den langsamen, ausgewertet während die Momentaufnahme zur Kerzenzeit eine flache Position zeigt, sendet einen Market-Verkauf über Volume. Ist stattdessen eine Long-Position offen, schließt dieselbe Kreuzung sie zunächst vollständig, und die daraus entstehende Schließorder löst sofort den Short-Einstieg aus.
- **Ausstieg**: Es gibt weder Stop noch Take-Profit noch einen Schutzblock: Eine Position wird bis zur entgegengesetzten Kreuzung gehalten, die sie über einen Close-position-Block vollständig schließt, dessen Volumen aus der Position abgeleitet wird. Der Berichtszweig beobachtet Ausführungen und Gewinn und gibt niemals eine Order auf, ersetzt oder storniert keine, sodass ein Abschalten der Benachrichtigungen das Handelsverhalten unverändert ließe.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzenserie; nur abgeschlossene Kerzen treiben die Indikatoren und jede darauf aufbauende Entscheidung an. |
| Fast EMA Length | 9 | Periode des schnellen ExponentialMovingAverage; er ist die schnellere Hälfte des Kreuzungspaares. |
| Slow EMA Length | 26 | Periode des langsamen ExponentialMovingAverage; er ist die langsamere Hälfte des Kreuzungspaares. |
| Volume | 1 | Menge, die den vier Einstiegsblöcken übergeben wird. Die beiden Schließblöcke ignorieren sie und nehmen ihre Größe aus der offenen Position. |
| Report Window Begin | 12:00:00 | Beginn des täglichen Berichtsfensters. Der erste Zeitpunkt darin löst den Statusbericht aus. |
| Report Window End | 12:05:00 | Ende des täglichen Berichtsfensters. Es muss nur breit genug sein, damit die Uhr einmal hineinfällt; das Flag beschränkt den Bericht unabhängig von der Breite auf eine einzige Zeile. |
| Day Reset Begin | 00:00:00 | Beginn des Rücksetzfensters, das das Flag löscht und am folgenden Tag einen neuen Bericht zulässt. |
| Day Reset End | 00:05:00 | Ende des Rücksetzfensters. Zwischen diesem Zeitpunkt und dem Beginn des Berichtsfensters bleibt der Zweig still. |
| Fill Report Caption | Trade report | Überschrift, die auf jede Ausführungsbenachrichtigung geschrieben wird; daran werden die Zeilen je Trade im Log erkannt. |
| Status Report Caption | Strategy status | Überschrift, die auf die tägliche Statusbenachrichtigung geschrieben wird; sie trennt sie von den Zeilen je Trade. |

## Diagrammdetails

- Der Block [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) gibt ausschließlich abgeschlossene Fünf-Minuten-Kerzen aus, und zwei [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Blöcke berechnen darauf ExponentialMovingAverage-Werte über 9 und 26. Die Filterung auf ausschließlich geformte Werte ist ausgeschaltet, beide Linien stehen also ab dem Beginn der Wiedergabe zur Verfügung und werden beide im Chart gezeichnet.
- Der Block [Crossing](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) feuert nur bei einer Kreuzung; eine NOT-[Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) macht aus seinem Abwärtsereignis einen positiven Auslöser. Die aktuelle [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) wird in einer [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) abgelegt, die einmal je Kerze freigegeben wird, und drei [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Blöcke machen daraus die Flags flat, long und short, die vier AND-Bedingungen mit der Kreuzung kombinieren.
- Sechs [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Blöcke arbeiten auf diesen vier Bedingungen: zwei Einstiege aus flat mit der Bedingung `OpenPosition`, zwei Schließungen mit der Bedingung `ClosePosition` und zwei weitere `OpenPosition`-Einstiege, die durch das Orderereignis der jeweils zugehörigen Schließung ausgelöst werden – genau das lässt eine Umkehr in zwei Schritten ablaufen. Alle sechs platzieren Market-Orders, und keiner von ihnen wartet auf eine Online-Verbindung.
- [Strategy trades](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) gibt jede eigene Ausführung aus. Ein [String Formatter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) stellt sie mit der Vorlage `Fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}` dar, und eine [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) vom Typ `Log` schreibt sie unter der Überschrift des Ausführungsberichts.
- [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) speist zwei [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)-Prüfungen; das Berichtsfenster setzt ein [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) und das Rücksetzfenster löscht es, und genau das begrenzt den Zweig auf einen Impuls pro Tag. Der Impuls gibt den realisierten Wert des Blocks [P&L](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) aus einer auf null vorbelegten Variablen frei, ein zweiter String Formatter schreibt `Daily status: realized result {0}`, und eine zweite `Log`-Benachrichtigung veröffentlicht ihn.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
