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
