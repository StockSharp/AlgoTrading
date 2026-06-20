# 12-Monats-Zyklus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Python-Strategie implementiert die 12-Monats-Zyklusanomalie. Aktien werden nach der Rendite gerankt, die sie vor einem Jahr im entsprechenden Kalendermonat erzielten. Jeden Monat wird das oberste Dezil gekauft und das unterste Dezil leerverkauft, wodurch ein marktneutrales Portfolio auf Basis der verzögerten Jahresperformance entsteht.

Das System verwendet Tagesdaten zur Annäherung an monatliche Schlusskurse und rebalanciert zu Beginn jedes Monats. Positionsgrößen werden skaliert, um die Dollar-Exposition auf der Long- und Short-Seite ausgewogen zu halten.

## Details

- **Universum**: benutzerdefinierte Liste von Wertpapieren.
- **Signal**: Sortierung nach der prozentualen Veränderung gegenüber dem gleichen Monat im Vorjahr.
- **Portfolio**: Long oberstes Dezil, Short unterstes Dezil mit Hebel pro Seite durch `Leverage` festgelegt.
- **Neugewichtung**: monatlich.
- **Daten**: Tageskerzen zu Monatsabschlusspreisen aggregiert.
