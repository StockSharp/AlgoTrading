# Strategiediagramm „Twenty Pips Once a Day“
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm geht höchstens eine Gegentrend-Position pro Tag ein. Einmal täglich, zu einer gewählten Uhrzeit und nur solange die Position glattgestellt ist, vergleicht es den Schlusskurs der abgeschlossenen Stundenkerze mit dem Schlusskurs der Kerze von vor 29 Balken und setzt gegen die Drift dieses Fensters: Nach einem Rückgang kauft es, nach einem Anstieg verkauft es. Ein kleiner Take-Profit, ein weiterer Stop-Loss und eine harte Altersgrenze für die Position übernehmen den Ausstieg.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Stundenkerzen treiben alles an. Innerhalb einer noch entstehenden Kerze wird nichts ausgewertet, jede Entscheidung fällt also auf einem geschlossenen Kurs.
- Previous value hält die Kerze von vor 29 Balken. Ihr Schlusskurs wird mit dem aktuellen Schlusskurs verglichen, was die Drift von etwa den letzten eineinviertel Tagen misst.
- Der Vergleich bestimmt die Seite gegen diese Drift: Liegt der ältere Schlusskurs über dem aktuellen, ist der Markt gefallen und das Schema kauft; liegt er darunter, ist der Markt gestiegen und das Schema verkauft. Es werden zwei strikte Vergleiche benutzt, sodass ein Fenster, das genau dort endet, wo es begonnen hat, überhaupt kein Signal erzeugt.
- Time liefert die Uhrzeit zur gerade geschlossenen Kerze, ein Converter entnimmt daraus die Stunde, und ein Vergleich mit dem Parameter Trading Hour öffnet das Einstiegsfenster für eine Kerze am Tag.
- Die aktuelle Position muss glattgestellt sein. Zusammen mit dem Stundenfilter für den einen Einstieg pro Tag und der Bedingung Open position an den Einstiegsblöcken hält das das Schema bei genau einer Position zur selben Zeit.
- Beide Einstiege sind Market-Orders mit festem Volumen. Ihre Ausführungen werden zusammengeführt und an Position protection übergeben, die die Position bei 0.1% Take-Profit oder 0.5% Stop-Loss schließt — dasselbe Verhältnis von eins zu fünf, auf dem die Idee aufgebaut ist.
- Ein N values-Zähler wird vom angenommenen Einstieg scharf geschaltet und zählt 21 abgeschlossene Kerzen. Läuft er ab, stellt ein auf Close position gesetzter Position modify-Block glatt, was noch offen ist, sodass eine Position, die keines der beiden Ziele erreicht hat, nicht unbegrenzt gehalten wird.
- Is trade allowed beobachtet die Handelsfreigabe der Plattform im Live-Betrieb. Bei jedem angenommenen Einstieg hält das Schema fest, wie diese Freigabe in diesem Moment stand, und schreibt eine Zeile ins Log; das ist ein Bericht und kein Veto: In einer historischen Wiedergabe wird die Freigabe nie erteilt, sodass eine Sperre des Einstiegs daran das gesamte Diagramm verstummen ließe.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Auf einer abgeschlossenen Stundenkerze, deren Uhrzeit-Stunde Trading Hour entspricht, bei glattgestellter Position und einem Schlusskurs von vor 29 Balken über dem aktuellen Schlusskurs: Kauf von Volume zum Marktpreis unter der Bedingung Open position.
- **Short-Einstieg**: Auf einer abgeschlossenen Stundenkerze, deren Uhrzeit-Stunde Trading Hour entspricht, bei glattgestellter Position und einem Schlusskurs von vor 29 Balken unter dem aktuellen Schlusskurs: Verkauf von Volume zum Marktpreis unter der Bedingung Open position.
- **Ausstieg**: Position protection schließt die Position bei 0.1% Take-Profit oder 0.5% Stop-Loss, gemessen ab der Einstiegsausführung, wobei der Schlusskurs der Kerze die Kursprüfungen speist. Wird keines der beiden Niveaus erreicht, löst der N values-Zähler 21 abgeschlossene Kerzen nach dem Einstieg aus und der Block Close position stellt den Rest glatt; hat die Absicherung die Position bereits geschlossen, findet diese Aktion nichts vor und tut nichts.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 01:00:00 | Zeitrahmen der Arbeitskerzen. Es werden nur abgeschlossene Kerzen verarbeitet, sodass eine Order nie innerhalb einer noch entstehenden Kerze datiert sein kann. |
| Lookback Bars | 29 | Wie viele Balken zurück der Referenz-Schlusskurs genommen wird. Das ist die Breite des Fensters, dessen Drift der Einstieg kontert. |
| Trading Hour | 7 | Uhrzeit-Stunde, zu der sich das tägliche Einstiegsfenster öffnet, gelesen aus der Strategiezeit, die die abgeschlossene Kerze begleitet. |
| Volume | 0.1 | Feste Stückzahl beider Einstiegsorders. Es gibt keine adaptive Positionsgrößenbestimmung: Jeder Einstieg hat dieselbe Größe. |
| Max Position Bars | 21 | Wie viele abgeschlossene Kerzen eine Position bestehen darf, bevor sie unabhängig von Gewinn oder Verlust glattgestellt wird. |
| Take Profit % | 0.1 | Günstige Bewegung, bei der Position protection die Position schließt, als Prozentsatz des Einstiegskurses. |
| Stop Loss % | 0.5 | Ungünstige Bewegung, bei der Position protection die Position schließt, als Prozentsatz des Einstiegskurses. Der Stop ist fest und zieht nicht nach. |

## Diagrammdetails

- Der Block [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) ist auf ausschließlich abgeschlossene Kerzen eingestellt, und [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) liegt auf der Kerze selbst statt auf einem Kurs, mit einem [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) dahinter. Zwei [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Blöcke machen aus den beiden Schlusskursen die Long- und die Short-Seite; da beide strikt sind, erzeugt ein unverändertes Fenster keine von beiden.
- [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) ist hier eine Datenquelle und kein Etikett: Ein Converter liest die Stunde aus, und ein Comparison gleicht sie mit einer [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) ab. Sowohl die Stunde als auch die Prüfung auf glattgestellte Position sind an die Kerze gebunden, denn die Konstanten, mit denen sie verglichen werden, werden vom Kerzenstrom ausgelöst; das Einstiegsgatter kann also nur einmal je abgeschlossener Kerze durchschalten.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) und ein Vergleich gegen null liefern die Prüfung auf Glattstellung, und die beiden [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)-Blöcke bündeln Drift, Stunde und Position zu je einem Signal pro Seite. Beide [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Blöcke tragen die Bedingung Open position, die zweite Absicherung gegen einen erneuten Einstieg bei offener Position.
- Das angenommene Signal schaltet außerdem [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) scharf; der Block zählt abgeschlossene Kerzen und löst dann einen dritten, auf Close position gesetzten Position modify-Block aus. Dieser Block führt kein Volumen: Die zu schließende Menge ergibt sich aus der offenen Position, und ohne Position entsteht schlicht keine Order.
- [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) führt die Ausführungen beider Einstiegsseiten für [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) zusammen. Parallel dazu gibt ein [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) genau einen Impuls je Einstieg ab und wird vom Alterszähler zurückgesetzt; dieser Impuls hält den Messwert von [Is trade allowed](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) in einer Variablen fest, aus der ein [String format](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html)-Block je eingegangener Position eine Log-Zeile per [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) macht.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
