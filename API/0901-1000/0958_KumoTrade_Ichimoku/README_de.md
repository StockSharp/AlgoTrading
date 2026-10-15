# KumoTrade Ichimoku-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf Ichimoku Cloud und Stochastic Oscillator.
Sie geht long, wenn der Preis über den Kijun zurückzieht, der Stochastic überverkauft ist und keine Wolke voraus liegt.
Sie geht short, wenn der Preis unter die Wolke fällt, der Stochastic überkauft ist und ein bärischer Kumo vorliegt.

## Details

- **Einstiegskriterien**:
  - Long: `Low > Kijun && Kijun > Tenkan && Close < SenkouA && StochD < 29`
  - Short: `Close < min(SenkouA, SenkouB) && High > Kijun && prevStochD > StochD >= 90`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - ATR-basierter Trailing-Stop
- **Stops**: Trailing stop mit ATR * 3
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochK` = 70
  - `StochD` = 15
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Ichimoku Cloud, Stochastic, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
