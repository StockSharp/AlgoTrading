# F&E-Ausgaben-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Querschnittsstrategie rankt Aktien nach ihrem Verhältnis von Forschungs- und Entwicklungsausgaben (R&D) zum Marktwert. Zu Beginn jedes Monats wird das oberste Quintil der Unternehmen mit der höchsten R&D-Intensität gekauft, während das unterste Quintil leerverkauft wird, in der Annahme, dass hohe R&D-Ausgaben zukünftige Outperformance vorhersagen.

Gewichte werden auf jeder Seite gleichmäßig verteilt und monatlich unter Verwendung täglicher Preisdaten neu gewichtet.

## Details

- **Universum**: Liste von Aktien mit R&D-Daten.
- **Signal**: R&D-Ausgaben geteilt durch Marktkapitalisierung.
- **Portfolio**: Long höchstes Quintil, Short niedrigstes Quintil.
- **Neugewichtung**: monatlich.
- **Risikokontrolle**: Handel übersprungen, wenn der Auftragswert unter `MinTradeUsd` liegt.
