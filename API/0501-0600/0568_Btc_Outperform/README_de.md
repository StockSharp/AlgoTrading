# BTC Outperform

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Handelsregeln

Fasst abgeschlossene UTC-Tageskerzen zu festen Perioden zusammen. Wochenperioden beginnen relativ zu Montag 1970-01-05, 90-Tage-Perioden relativ zu 1970-01-01. Ein Schlusskurs wird erst veröffentlicht, wenn alle Tageskerzen der gesamten Periode vorhanden sind. Unvollständige Startperioden und Perioden mit Lücken veröffentlichen keinen Schluss.

Verglichen werden die letzten vollständig beobachteten abgeschlossenen Schlusskurse: Woche höher kauft, niedriger verkauft, Gleichheit behält die Position. Ausgehend von null beträgt die Order `Volume`; eine Umkehr verwendet `Volume+abs(Position)` und hinterlässt die volle Gegenposition. Laufende Perioden verändern den Vergleich nicht. Offene Orders verhindern Duplikate. Kein automatischer Preisstop oder Gewinnziel.

## Parameter

| Parameter | Standardwert |
|---|---|
| `CandleType` | `1d` |
| `WeeklyDays` | `7` |
| `QuarterlyDays` | `90` |
