# Strategiediagramm „Two Session Open Time“
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm enthält überhaupt keinen Indikator: Die Uhr ist die einzige Signalquelle. Zwei getrennte Zeitfenster des Handelstages eröffnen jeweils eine Long-Position, ein Flag begrenzt jedes Fenster auf einen einzigen Einstieg pro Tag, und ein drittes Fenster stellt glatt, was noch offen ist, und schärft beide Fenster für den folgenden Tag erneut.

![schema](schema.svg)

## Strategieübersicht

- Time leitet den aktuellen Zeitpunkt an drei Working-time-Blöcke weiter: zwei Einstiegsfenster, 09:30-14:00 und 00:00-04:00, sowie ein Zwangsschließungsfenster, 19:50-20:00.
- Jedes Einstiegsfenster wird über eine auf And gesetzte Logical condition mit einer Prüfung auf eine flache Position verknüpft, sodass ein Fenster einen Einstieg nur anfordern kann, solange nichts offen ist.
- Ein Fenster bleibt stundenlang offen, und sein Gate wiederholt dabei immer denselben wahren Wert. Flag steht zwischen Gate und Order und lässt nur den ersten davon durch, wodurch aus einem langen Fenster ein einziger Einstieg wird.
- Beide Fenster kaufen. Position modify arbeitet mit der Bedingung Open position, sodass eine Market-Order über Order Volume nur dann herausgeht, wenn die Position exakt null ist.
- Das Zwangsschließungsfenster steuert ein drittes Position modify, das auf Close position gesetzt ist, und genau dasselbe Signal setzt beide Flags zurück, sodass die beiden Einstiegsfenster für den nächsten Tag wieder scharf sind.
- Position protection beobachtet die Ausführungen beider Einstiege und schließt die Position bei einem Take-Profit von 1.5% oder einem Trailing-Stop von 0.5%, der dem Kerzenschluss folgt.
- Abgeschlossene Fünf-Minuten-Kerzen geben dem gesamten Diagramm den Takt: Sie liefern den Schlusskurs an Position protection, sie sind das, was das Panel zeichnet, und ihr Eintreffen bewegt die Uhr voran.
- Das Chart-Panel zeigt die Kerzen, die Kurslinie, auf die die Absicherung reagiert, jede vom Diagramm gesendete Order und jede eingehende Ausführung.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Innerhalb eines der beiden Fenster gibt das Flag dieses Fensters bei flacher Position sein erstes wahres Signal frei, und Position modify kauft unter der Bedingung Open position Order Volume zum Marktpreis. Jedes spätere Signal desselben Fensters wird vom Flag verschluckt, bis das Schließungsfenster es zurücksetzt.
- **Short-Einstieg**: Es gibt keine Short-Seite. Beide Fenster eröffnen long, und die einzigen Verkaufsorders, die das Diagramm überhaupt sendet, sind jene, die eine offene Long-Position schließen.
- **Ausstieg**: Position protection schließt die Position bei einem Take-Profit von 1.5% oder einem Trailing-Stop von 0.5%, der dem Kerzenschluss folgt. Was zu Beginn des Schließungsfensters noch offen ist, wird von der Aktion Close position glattgestellt, die zugleich beide Latches löscht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Fünf-Minuten-Zeitrahmen; verarbeitet werden nur abgeschlossene Kerzen, und ihre Schlusskurse sind die Grundlage sowohl für die Kursprüfung der Absicherung als auch für die Chartlinie. |
| First Window From | 09:30:00 | Beginn des ersten Einstiegsfensters in Wiedergabe- oder Serverzeit. |
| First Window Until | 14:00:00 | Ende des ersten Einstiegsfensters; danach kann dieses Fenster keinen Einstieg mehr scharfstellen. |
| Second Window From | 00:00:00 | Beginn des zweiten Einstiegsfensters in Wiedergabe- oder Serverzeit. |
| Second Window Until | 04:00:00 | Ende des zweiten Einstiegsfensters. |
| Close Window From | 19:50:00 | Beginn des Zwangsschließungsfensters, das eine offene Position glattstellt und beide Latches zurücksetzt. |
| Close Window Until | 20:00:00 | Ende des Zwangsschließungsfensters. |
| Order Volume | 1 | Feste Stückzahl, die von beiden Fenstereinstiegen verwendet wird. |
| Take Profit, % | 1.5 | Günstige prozentuale Kursbewegung, bei der Position protection die Position schließt. |
| Stop Loss, % | 0.5 | Ungünstige prozentuale Bewegung des Stops; die Trailing-Technik zieht ihn hinter dem Kerzenschluss nach, sobald der Kurs zugunsten der Position läuft. |

## Diagrammdetails

- Der Block [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) gibt abgeschlossene Fünf-Minuten-Kerzen aus. Ein [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) nimmt deren Schlusskurs — genau den Kurs, an dem [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) Take-Profit und Trailing-Stop misst und der als Linie neben den Kerzen gezeichnet wird. Mehr wird aus dem Kurs nicht berechnet: Das Diagramm enthält keinen Indikator.
- [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) liefert den aktuellen Zeitpunkt an drei [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)-Blöcke. Zwei davon markieren die Einstiegsfenster, einer das Zwangsschließungsfenster; die Wiedergabe der mitgelieferten Historie läuft in UTC, daher werden die Fenstergrenzen als UTC-Zeiten gelesen.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html), über [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) mit einer auf null gesetzten [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) verglichen, ergibt die Flat-Prüfung, die sich beide And-Gates der [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) teilen; eine offene Position blockiert damit stillschweigend auch das jeweils andere Fenster.
- [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) macht aus einem Fenster ein einzelnes Ereignis. Sein Trigger ist das And-Gate, sein Reset das Schließungsfenster; es gibt einen Wert nur in dem Moment weiter, in dem es gesetzt wird, sodass die Hunderte wahren Messwerte eines vierstündigen Fensters zu einem einzigen Einstieg zusammenfallen.
- Drei [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Blöcke werden aktiv: zwei Einstiege mit Open position, die Order Volume nehmen, und eine Glattstellung mit Close position, die ohne Volumen auskommt, weil sie die Position ausliest, die sie auflösen soll. Beide Einstiegsausführungen werden von [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) zusammengeführt und an Position protection übergeben, dessen eigene schließende Ausführung zwar auf dem Panel gezeichnet, aber nicht in seinen Trade-Eingang zurückgeführt wird.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
