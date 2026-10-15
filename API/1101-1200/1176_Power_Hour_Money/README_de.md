# Power Hour Money Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt während ausgewählter New Yorker Sitzungen und eröffnet Positionen, wenn alle wichtigen Zeitrahmen übereinstimmen.
Eine Long-Position wird eröffnet, wenn Monats-, Wochen-, Tages- und Stundenkerzen höher als ihre Eröffnung schließen.
Eine Short-Position wird eröffnet, wenn alle unter der Eröffnung schließen.
Optionale Trailing-Stops schützen Gewinne, und Positionen können um 16:45 geschlossen werden.

## Details

- **Einstieg**: Long wenn alle Zeitrahmen grün sind, Short wenn alle rot sind.
- **Sitzungsfilter**: NY 9:30-11:30, erweitert 8:00-16:00 oder alle Sitzungen.
- **Trailing-Stop**: prozentbasiert für Long- und Short-Seite.
- **Tagesende**: optionales Schließen aller Positionen um 16:45.

## Berechnung und Ausführung

C# und Python verwenden dasselbe kausale Modell: Nach jeder abgeschlossenen Basiskerze ist deren Schlusskurs der aktuell bekannte Schlusskurs der laufenden Monats-, Wochen-, Tages- und Stundenkerze. Er wird strikt mit jeder Eröffnung verglichen. Gemischte Farben oder Gleichheit halten die Position. Zukünftige endgültige Schlusskurse der höheren Zeitrahmen werden nie gelesen.

Monat, Woche ab Montag und Tag folgen dem New Yorker Kalender einschließlich Sommerzeit. Stunden werden nach UTC unterschieden, damit die doppelte Herbststunde getrennt bleibt. Nach einem beobachteten Periodenwechsel liefert die erste verfügbare Basiskerze die Eröffnung. Eine bei Beginn der Historie bereits laufende Periode bleibt bis zur nächsten Grenze unbekannt; der erste beobachtete Preis ersetzt keine fehlende Eröffnung. Vor dem Einstieg müssen alle vier Eröffnungen bekannt sein.

`CandleType` ist standardmäßig fünf Minuten. `SessionStart=09:30` und `SessionEnd=11:30` gelten in New Yorker Marktzeit, mit eingeschlossenem Beginn und ausgeschlossenem Ende. Für die erweiterte Sitzung `08:00`–`16:00` einstellen oder `IsAllSessions=true` wählen. `IsAllSessions`, `IsTrailingEnabled` und `IsEndOfDayCloseEnabled` sind standardmäßig `false`; `LongTrailingPercent=2`, `ShortTrailingPercent=2`, `EndOfDayTime=16:45`.

Gleiche Farben eröffnen aus einer Nullposition `Volume` oder kehren vollständig mit `Volume + abs(Position)` um. Eine ausstehende Order verhindert weitere Orders; es gibt keine Wartefrist. Optionales Trailing beginnt beim tatsächlichen Ausführungspreis: Long schließt, wenn der aktuelle Bid `höchster beobachteter Bid * (1 - LongTrailingPercent / 100)` erreicht; Short bei Ask `niedrigster beobachteter Ask * (1 + ShortTrailingPercent / 100)`. Ausführungspreise aktualisieren ebenfalls das günstige Extrem. Beide Prozentsätze liegen strikt zwischen 0 und 100. Eine Umkehr setzt das Extrem zurück.

Der optionale Tagesschluss sendet beim ersten Marktzeitereignis ab dem Grenzzeitpunkt eine Marktorder für die gesamte Position und sperrt Einstiege bis Mitternacht in New York. Quotierungen und ein Marktzeittimer von einer Minute prüfen unabhängig von Sitzungen und Kerzenaufwärmung. Diese Ausstiege benötigen Marktereignisse und garantieren bei Kurslücken keinen exakten Ausführungspreis.
