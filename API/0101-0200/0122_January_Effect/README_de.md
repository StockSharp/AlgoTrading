# Januar-Effekt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt den Januareffekt über den Jahreswechsel. Einstieg auf der ersten verfügbaren abgeschlossenen Kerze des ersten Handelstages ab dem 26. Dezember. Ausstieg am Ende der ersten Sitzung ab dem 15. Januar oder auf der nächsten geeigneten Kerze, falls die Schlusskerze fehlt. Keine monatliche Wiederholung und keine Short-Einstiege.

Verwendet abgeschlossene 15-Minuten-Kerzen und den ausdrücklich gesetzten Handelskalender von `Security.Board`, einschließlich Feiertagen, Sondersitzungen und Zeitzone des Handelsplatzes. Ohne Kalender ist kein Start möglich. Nur Long-Positionen, höchstens ein Einstiegsversuch je Jahressaison; nach einem Stop erfolgt in dieser Saison kein Wiedereinstieg. Ausstehende Aufträge blockieren überlappende Entscheidungen.

`StopLoss = 2` setzt lokalen nativen Schutz von 2% ab dem tatsächlichen Einstiegskurs. Bid/Ask-Aktualisierungen können zwischen Kerzen einen Marktausstieg auslösen. Kein Take-Profit.
