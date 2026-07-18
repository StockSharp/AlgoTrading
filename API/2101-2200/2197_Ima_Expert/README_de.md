# Ima Expert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die auf der relativen Geschwindigkeit des Preises gegenüber seinem gleitenden Durchschnitt basiert.
Das Verhältnis `Close / SMA - 1` wird zwischen zwei aufeinanderfolgenden Kerzen verglichen. Ein starker Anstieg öffnet eine Long-Position, während ein starker Rückgang eine Short-Position öffnet.

## Details

- **Einstiegskriterien**:
  - Long: `(IMA_now - IMA_prev) / abs(IMA_prev) >= SignalLevel`
  - Short: `(IMA_now - IMA_prev) / abs(IMA_prev) <= -SignalLevel`
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Positionsgröße**: `RiskLevel` und `StopLossTicks` bestimmen das Handelsvolumen, begrenzt durch `MaxVolume`
- **Long/Short**: Beide
- **Stops**: Keine
- **Standardwerte**:
  - `SmaPeriod` = 5
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 1000
  - `SignalLevel` = 0.5
  - `RiskLevel` = 0.01
  - `MaxVolume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
