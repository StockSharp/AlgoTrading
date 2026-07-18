# RoNz Schnellfeuer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen gleitenden Durchschnitt mit dem Parabolic-SAR-Indikator, um schnelle Trendwechsel zu erkennen. Eine Long-Position wird eröffnet, wenn der Schlusskurs über dem gleitenden Durchschnitt liegt und der Parabolic SAR unter den Preis wechselt. Eine Short-Position wird bei umgekehrten Bedingungen eröffnet. Positionen können optional gemittelt werden, wenn der Trend anhält.

## Funktionsweise
- **Einstieg Long**: Schlusskurs > SMA und Parabolic SAR wechselt unter den Preis.
- **Einstieg Short**: Schlusskurs < SMA und Parabolic SAR wechselt über den Preis.
- **Schließen**: Entweder per Stop-Loss/Take-Profit oder per entgegengesetztem Signal je nach gewähltem Modus.
- **Mittelwertbildung**: Neue Positionen werden hinzugefügt, wenn der Trend anhält.
- **Trailing Stop**: Passt den Stop-Preis an, wenn sich der Trade im Gewinn bewegt.

## Parameter
- `Volume` – Handelsvolumen.
- `StopLoss` – Stop-Loss in Ticks.
- `TakeProfit` – Take-Profit in Ticks.
- `TrailingStop` – Trailing Stop in Ticks.
- `Averaging` – Positionsmittelung aktivieren.
- `MaPeriod` – Periode des gleitenden Durchschnitts.
- `PsarStep` – Schritt des Parabolic SAR.
- `PsarMax` – Maximalwert des Parabolic SAR.
- `CloseType` – `SlClose` verwendet nur Stops, `TrendClose` schließt bei entgegengesetztem Trend.
- `CandleType` – Kerzenserie für Berechnungen.

## Hinweise
- Funktioniert mit jedem von StockSharp unterstützten Instrument.
- Erfordert historische Kerzen für den ausgewählten `CandleType`.
