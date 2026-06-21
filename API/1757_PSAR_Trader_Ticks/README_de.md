# PSAR Trader Ticks-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Parabolic SAR-Indikator. PSAR Trader Ticks folgt den Punkten des Parabolic SAR-Indikators und reagiert, wenn der Preis von einer Seite zur anderen kreuzt. Es eröffnet eine Long-Position, wenn der Preis über den SAR steigt, und eine Short-Position, wenn der Preis unter ihn fällt. Der Handel kann auf einen bestimmten Zeitbereich beschränkt werden, und bestehende Positionen können optional geschlossen werden, wenn ein gegenteiliges Signal erscheint. Die Strategie wendet auch Take-Profit- und Stop-Loss-Niveaus an, die in Ticks gemessen werden.

## Details

- **Einstiegskriterien**: Preis, der den Parabolic SAR-Indikator kreuzt.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal (optional), Stop-Loss oder Take-Profit.
- **Stops**: Take-Profit und Stop-Loss in Ticks.
- **Standardwerte**:
  - `Step` = 0.001m
  - `Maximum` = 0.2m
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Parabolic SAR
  - Stops: Take-Profit, Stop-Loss
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
