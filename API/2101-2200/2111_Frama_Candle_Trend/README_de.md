# FrAMA Candle Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den MetaTrader-Experten *Exp_FrAMACandle* in eine StockSharp-High-Level-Strategie.

## Strategielogik

- Verwendet den **Fractal Adaptive Moving Average (FrAMA)**, der separat für die Eröffnungs- und Schlusskurse der Kerzen berechnet wird.
- Ein bullisches Signal tritt auf, wenn der FrAMA des Schlusskurses über den FrAMA des Eröffnungskurses steigt. Falls der vorherige Balken nicht bullisch war, öffnet die Strategie eine Long-Position und schließt bestehende Shorts.
- Ein bärisches Signal tritt auf, wenn der FrAMA des Schlusskurses unter den FrAMA des Eröffnungskurses fällt. Falls der vorherige Balken nicht bärisch war, öffnet die Strategie eine Short-Position und schließt bestehende Longs.
- Signale werden nur bei abgeschlossenen Kerzen ausgewertet. Historische Farbwerte werden gespeichert, um den `SignalBar`-Versatz zu berücksichtigen.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen für die Indikatorberechnung. Standard: 4 Stunden. |
| `FramaPeriod` | Periode des FrAMA-Indikators. |
| `SignalBar` | Versatz des Balkens zur Signalerkennung. |
| `BuyOpen` / `SellOpen` | Eröffnung von Long-/Short-Positionen aktivieren. |
| `BuyClose` / `SellClose` | Schließen von Long-/Short-Positionen aktivieren. |

## Hinweise

- Die Strategie basiert ausschließlich auf FrAMA-Kreuzungen und implementiert kein Stop-Loss- oder Take-Profit-Management.
- Das Positionsvolumen wird durch die Basis-`Volume`-Eigenschaft der Strategie gesteuert.
