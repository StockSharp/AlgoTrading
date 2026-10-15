# Volatilitätsqualitäts-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Beispielstrategie, die zeigt, wie man mit Richtungswechseln eines geglätteten Medianpreises handelt. Der ursprüngliche MQL-Experte verwendete den *Volatility Quality*-Indikator; diese Implementierung nähert ihn mit einem einfachen gleitenden Durchschnitt des Medianpreises an.

## Strategielogik
- Den Medianpreis jeder Kerze `(High + Low) / 2` berechnen.
- Den Medianpreis mit einem einfachen gleitenden Durchschnitt (SMA) glätten.
- Die Indikatorfarbe bestimmen: steigende Werte werden als **auf** (Farbe 0) und fallende Werte als **ab** (Farbe 1) behandelt.
- Wenn die Farbe von auf nach ab wechselt, schließt die Strategie jede Short-Position und eröffnet eine Long-Position.
- Wenn die Farbe von ab nach auf wechselt, schließt die Strategie jede Long-Position und eröffnet eine Short-Position.
- Grundlegendes Risikomanagement wird über feste Stop-Loss- und Take-Profit-Niveaus angewendet.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `Length` | Glättungsperiode für den SMA, angewendet auf den Medianpreis. |
| `Candle Type` | Zeitrahmen der für Berechnungen verwendeten Kerzen. |

## Haftungsausschluss
Dieses Beispiel dient zu Lehrzwecken. Es vereinfacht den ursprünglichen Algorithmus und kann sich anders verhalten als die MQL-Version. Verwendung auf eigenes Risiko.
