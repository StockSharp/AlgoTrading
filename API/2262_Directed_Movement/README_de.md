# Gerichtete-Bewegung-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie repliziert den Expertenberater **Directed Movement** aus MetaTrader. Sie wendet einen Relative Strength Index (RSI) an, der zweimal durch gleitende Durchschnitte geglättet wird. Die erste Glättung bildet eine schnelle Linie, während die zweite Glättung eine langsamere Linie erzeugt.

Handelsentscheidungen basieren auf dem Kreuzungsverhalten der schnellen und langsamen Linie im konträren Stil:

- **Kaufen**, wenn die schnelle Linie unter die langsame Linie kreuzt.
- **Verkaufen**, wenn die schnelle Linie über die langsame Linie kreuzt.

Optionale Stop-Loss- und Take-Profit-Niveaus werden als Prozentsätze des Einstiegspreises angewendet.

## Indikatoren

- `RelativeStrengthIndex` – Basis-Momentum-Indikator.
- `MovingAverage` – erste Glättung des RSI (schnelle Linie).
- `MovingAverage` – zweite Glättung der schnellen Linie (langsame Linie).

## Handelsregeln

1. RSI aus Kerzenschlusskursen berechnen.
2. RSI mit dem ersten gleitenden Durchschnitt glätten, um die schnelle Linie zu erhalten.
3. Schnelle Linie mit dem zweiten gleitenden Durchschnitt glätten, um die langsame Linie zu erhalten.
4. Long-Position eingehen, wenn die schnelle Linie unter die langsame Linie kreuzt. Vorher bestehende Short-Position schließen.
5. Short-Position eingehen, wenn die schnelle Linie über die langsame Linie kreuzt. Vorher bestehende Long-Position schließen.
6. Stop-Loss- und Take-Profit-Schutz anwenden, wenn ihre Parameter größer als null sind.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `CandleType` | Für Berechnungen verwendete Kerzenserie. |
| `RsiPeriod` | RSI-Berechnungsperiode. |
| `FirstMaType` | Art des gleitenden Durchschnitts für die schnelle Linie. |
| `FirstMaLength` | Periode des schnellen gleitenden Durchschnitts. |
| `SecondMaType` | Art des gleitenden Durchschnitts für die langsame Linie. |
| `SecondMaLength` | Periode des langsamen gleitenden Durchschnitts. |
| `StopLossPercent` | Stop-Loss in Prozent des Einstiegspreises. |
| `TakeProfitPercent` | Take-Profit in Prozent des Einstiegspreises. |

