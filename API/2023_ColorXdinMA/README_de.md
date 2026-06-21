# ColorXdinMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die ColorXdinMA-Strategie implementiert den XdinMA-Indikator, berechnet als `ma_main * 2 - ma_plus`, wobei beide Komponenten einfache gleitende Durchschnitte mit unterschiedlichen Längen sind. Die Strategie überwacht die Steigung dieser Linie und eröffnet Positionen, wenn sich die Steigungsrichtung ändert.

## Handelslogik
- Wenn der Indikator sinkend war und nach oben dreht, wird eine Long-Position eröffnet. Bestehende Short-Positionen werden geschlossen.
- Wenn der Indikator steigend war und nach unten dreht, wird eine Short-Position eröffnet. Bestehende Long-Positionen werden geschlossen.

Nur abgeschlossene Kerzen werden verarbeitet. Orders werden als Marktorders ausgeführt.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `MainLength` | Periode des gleitenden Hauptdurchschnitts. | 10 |
| `PlusLength` | Periode des zusätzlichen gleitenden Durchschnitts. | 20 |
| `CandleType` | Zeitrahmen der für die Berechnung verwendeten Kerzen. | 6 Stunden |

## Hinweise
Diese Implementierung ist eine High-Level-StockSharp-Strategie und kann bei Bedarf mit Risikomanagement- oder Visualisierungsfunktionen erweitert werden.
