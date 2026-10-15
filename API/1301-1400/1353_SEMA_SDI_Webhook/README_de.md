# SEMA SDI Webhook-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf geglättetem EMA-Crossover und Bestätigung durch den geglätteten Richtungsindex.
Kauft wenn +DI > -DI und schnelle EMA > langsame EMA. Verkauft wenn -DI > +DI und schnelle EMA < langsame EMA.

## Details

- **Einstiegskriterien**:
  - Long: `+DI > -DI && FastEMA > SlowEMA`
  - Short: `+DI < -DI && FastEMA < SlowEMA`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Take-Profit, Stop-Loss, Trailing
- **Stops**: TP, SL, Trailing
- **Standardwerte**:
  - `FastEmaLength` = 58
  - `SlowEmaLength` = 70
  - `SmoothLength` = 3
  - `DiLength` = 1
  - `TakeProfitPercent` = 25
  - `StopLossPercent` = 4.8
  - `TrailingPercent` = 1.9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, Directional Index
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
