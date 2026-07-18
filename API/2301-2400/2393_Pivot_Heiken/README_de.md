# Pivot Heiken-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die tägliche Pivot-Punkte mit Heikin-Ashi-Kerzen und einem optionalen Trailing Stop kombiniert. Der Tages-Pivot wird aus dem Hoch, Tief und Schlusskurs des Vortages berechnet. Die Heikin-Ashi-Glättung filtert Preisrauschen und hebt die Trendrichtung hervor.

## Logik
- **Long-Einstieg**: Heikin-Ashi-Kerze ist bullisch und der Schlusskurs liegt über dem Tages-Pivot.
- **Short-Einstieg**: Heikin-Ashi-Kerze ist bärisch und der Schlusskurs liegt unter dem Tages-Pivot.
- **Ausstieg**: Position verlässt auf Stop-Loss-, Take-Profit- oder Trailing-Stop-Niveau.

## Parameter
- `CandleType` – verwendete Kerzenserie.
- `StopLossPips` – Stop-Loss-Abstand in Pips.
- `TakeProfitPips` – Take-Profit-Abstand in Pips.
- `TrailingStopPips` – Trailing-Stop-Abstand in Pips (0 deaktiviert Trailing).

## Indikatoren
- Heikin-Ashi (intern berechnet).
- Täglicher Pivot-Punkt.

## Hinweise
- Verwendet die High-Level-API mit Kerzenabonnements und Indikator-Binding.
- Geeignet sowohl für Long- als auch Short-Handel.
