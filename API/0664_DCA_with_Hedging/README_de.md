# DCA-Strategie mit Hedging
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht Long, nachdem drei aufeinanderfolgende Kerzen über dem EMA schließen, und geht Short, nachdem drei aufeinanderfolgende Kerzen darunter schließen. Zusätzliche Positionen werden hinzugefügt, wenn sich der Preis um einen bestimmten Prozentsatz gegen den letzten Einstieg bewegt. Die gesamte Position wird geschlossen, sobald sich der Preis um den Take-Profit-Prozentsatz vom durchschnittlichen Einstiegspreis entfernt.

## Parameter
- Kerzentyp
- EMA-Länge
- DCA-Intervall %
- Take-Profit %
- Anfängliche Positionsgröße

