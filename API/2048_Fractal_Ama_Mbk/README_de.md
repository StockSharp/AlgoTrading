# Fractal AMA MBK-Kreuzungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Fractal AMA MBK-Kreuzungsstrategie verwendet den **Fractal Adaptive Moving Average (FRAMA)** zusammen mit einer **Exponential Moving Average (EMA)**-Triggerlinie. Handelssignale werden generiert, wenn die FRAMA-Linie die EMA-Linie kreuzt.

## Funktionsweise
- FRAMA passt seinen Glättungsfaktor basierend auf der Fraktaldimension der jüngsten Preisbewegung an.
- Die EMA dient als Triggerlinie, die Preisdaten glättet.
- **Long-Einstieg:** wenn FRAMA die EMA von unten nach oben kreuzt und keine Long-Position geöffnet ist.
- **Short-Einstieg:** wenn FRAMA die EMA von oben nach unten kreuzt und keine Short-Position geöffnet ist.
- Bestehende Positionen können mit optionalen Stop-Loss- und Take-Profit-Niveaus geschützt werden.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Kerzentyp und Zeitrahmen für Berechnungen (Standard: 4-Stunden-Kerzen). |
| `FramaPeriod` | Periodenlänge für den FRAMA-Indikator. |
| `SignalPeriod` | Periodenlänge für die EMA-Triggerlinie. |
| `StopLoss` | Stop-Loss-Abstand vom Einstiegspreis in absoluten Preiseinheiten (0 deaktiviert). |
| `TakeProfit` | Take-Profit-Abstand vom Einstiegspreis in absoluten Preiseinheiten (0 deaktiviert). |
| `Volume` | Handelsvolumen in Lots. |

## Hinweise
- Es werden nur abgeschlossene Kerzen verarbeitet.
- Trades werden mit Marktorders ausgeführt (`BuyMarket`/`SellMarket`).
- Die Parameter `FramaPeriod` und `SignalPeriod` unterstützen Optimierung.
