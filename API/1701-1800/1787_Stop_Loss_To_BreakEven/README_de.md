# Strategie Stop-Loss auf Break-Even verschieben
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verschiebt den Schutz-Stop-Loss auf den Einstiegspreis, sobald die Position einen bestimmten Gewinn in Pips erreicht. Sie ist nützlich zum Sichern von Gewinnen, ohne Orders manuell anpassen zu müssen.

## Funktionsweise

- Überwacht den Preis mit dem ausgewählten Kerzentyp.
- Wenn der aktuelle Positionsgewinn die konfigurierte Pip-Anzahl überschreitet, wird eine Stop-Order beim Einstiegspreis platziert.
- Funktioniert sowohl für Long- als auch Short-Positionen und berechnet die Pip-Größe automatisch anhand des Preisschritts des Instruments.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| **BreakEvenPips** | Gewinn in Pips, der erforderlich ist, bevor der Stop-Loss auf den Einstiegspreis verschoben wird. |
| **CandleType** | Kerzentyp zur Überwachung der Kursbewegungen. |

## Hinweise

Die Strategie generiert keine Einstiegssignale. Positionen sollten durch andere Strategien oder manuell geöffnet werden. Nach Schließung der Position wird der interne Zustand zurückgesetzt, um auf den nächsten Trade zu warten.
