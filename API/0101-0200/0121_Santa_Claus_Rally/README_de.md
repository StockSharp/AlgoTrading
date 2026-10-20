# Santa Claus Rally Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt die Jahresendrally. Einstieg auf der ersten geeigneten abgeschlossenen Kerze innerhalb der letzten fünf Dezember-Handelssitzungen. Ausstieg am Ende der zweiten Januar-Handelssitzung oder auf der ersten späteren geeigneten Kerze, falls die Schlusskerze fehlt.

Verwendet abgeschlossene 15-Minuten-Kerzen und den ausdrücklich gesetzten Handelskalender von `Security.Board`, einschließlich Feiertagen, Sondersitzungen und Zeitzone des Handelsplatzes. Ohne Kalender ist kein Start möglich. Nur Long-Positionen, höchstens ein Einstiegsversuch je Jahressaison; nach einem Stop erfolgt in dieser Saison kein Wiedereinstieg. Ausstehende Aufträge blockieren überlappende Entscheidungen.

`StopLoss = 2` setzt lokalen nativen Schutz von 2% ab dem tatsächlichen Einstiegskurs. Bid/Ask-Aktualisierungen können zwischen Kerzen einen Marktausstieg auslösen. Kein Take-Profit.

Die Saison wird beim Absenden des Einstiegs gespeichert. Eine offene Position behält die Januar-Ausstiegsfrist dieser Saison auch dann, wenn die nächste verfügbare Kerze erst im Dezember eines späteren Jahres eintrifft; sie wird dann geschlossen statt in eine neue Saison verschoben.
