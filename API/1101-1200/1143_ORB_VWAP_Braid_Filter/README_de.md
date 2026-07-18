# ORB VWAP Braid Filter Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opening-Range-Ausbruch-Strategie mit VWAP- und Braid-Filter-Bestätigung.

## Regeln
- Handel zwischen 09:35 und 11:00 Uhr Börsenzeit
- Ein Trade pro Tag
- Long, wenn der Kurs über dem Opening-Range-Hoch, über dem VWAP schließt und der Braid-Filter bullisch ist
- Short, wenn der Kurs unter dem Opening-Range-Tief, unter dem VWAP schließt und der Braid-Filter bärisch ist
- Stop-Loss auf der gegenüberliegenden Seite der Range
- Take-Profit beim Zweifachen des Risikos, begrenzt durch Vortages- oder Vorbörsenniveaus

## Indikatoren
- Volumengewichteter gleitender Durchschnitt (VWAP)
- Exponentieller gleitender Durchschnitt (3, 7, 14)
- Average True Range (14)
