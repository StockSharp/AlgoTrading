# Strategie Ma Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie - MA + Williams %R. Kaufen, wenn der Preis über der MA liegt und der Williams %R unter -80 (überverkauft) ist. Verkaufen, wenn der Preis unter der MA liegt und der Williams %R über -20 (überkauft) ist.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 79%. Die Strategie funktioniert am besten auf dem Aktienmarkt.

Der gleitende Durchschnitt zeigt die vorherrschende Trendrichtung. Williams %R sucht nach überkauften oder überverkauften Punkten relativ zu diesem Trend.

Passt zu Swing-Tradern, die auf Rücksetzer in Richtung des Durchschnitts warten. Der Stop-Loss-Abstand wird vom ATR abgeleitet.

## Details

- **Einstiegskriterien**:
  - Long: `Close > MA && WilliamsR < WilliamsROversold`
  - Short: `Close < MA && WilliamsR > WilliamsROverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Williams %R kehrt zur Mitte zurück
- **Stops**: Prozentbasiert mit `StopLoss`
- **Standardwerte**:
  - `MaPeriod` = 20
  - `MaType` = MovingAverageTypeEnum.Simple
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Moving Average, Williams %R, R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

