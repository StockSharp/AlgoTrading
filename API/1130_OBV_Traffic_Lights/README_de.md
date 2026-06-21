# OBV Traffic Lights-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet ein auf Heikin Ashi basierendes On-Balance Volume und drei wie Ampeln eingefärbte EMAs. Long, wenn OBV und die schnelle EMA über der langsamen EMA liegen; Short, wenn beide darunter liegen. Positionen werden geschlossen, wenn die Bedingungen wegfallen.

- **Einstiegskriterien**: OBV > langsame EMA und schnelle EMA > langsame EMA für Long; OBV < langsame EMA und schnelle EMA < langsame EMA für Short.
- **Ausstiegskriterien**: Gegenseitiges Signal oder Verlust der Übereinstimmung.
- **Indikatoren**: OBV, EMA, Highest/Lowest
