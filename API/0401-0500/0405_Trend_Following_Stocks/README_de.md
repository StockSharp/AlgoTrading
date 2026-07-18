# Trendfolge-Strategie für Aktien
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt einzelne Aktien mithilfe eines einfachen Trendfilters. Aktien, die über einem gleitenden Durchschnitt notieren, werden gekauft; jene darunter werden gemieden oder leerverkauft.

Das Portfolio wird wöchentlich mit gleicher Positionsgröße aktualisiert, und Trailing-Stops schützen das Kapital.

## Details

- **Daten**: Tägliche Aktien-Schlusskurse.
- **Einstieg**: Kauf, wenn Kurs > gleitender Durchschnitt; Short, wenn darunter.
- **Ausstieg**: Kurs kreuzt den Durchschnitt zurück oder Stop wird ausgelöst.
- **Instrumente**: Liquide Aktien.
- **Risiko**: Trailing-Stop und Positionsobergrenze.

