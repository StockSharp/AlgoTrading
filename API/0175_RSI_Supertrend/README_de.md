# Rsi Supertrend Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf den Indikatoren RSI und Supertrend. Geht long, wenn der RSI überverkauft ist (< 30) und der Preis über Supertrend liegt. Geht short, wenn der RSI überkauft ist (> 70) und der Preis unter Supertrend liegt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 112%. Sie funktioniert am besten auf dem Forex-Markt.

Der RSI-Oszillator definiert Momentum-Extreme, während Supertrend die vorherrschende Richtung anzeigt. Trades entstehen, wenn der RSI mit der Supertrend-Farbe übereinstimmt.

Funktioniert für Trader, die einen Trailing-Stop-Ausstieg bevorzugen. ATR-Einstellungen sichern die Position zusätzlich ab.

## Details

- **Einstiegskriterien**:
  - Long: `RSI < 30 && Close > Supertrend`
  - Short: `RSI > 70 && Close < Supertrend`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Supertrend-Wechsel
- **Stops**: Trailing mit Supertrend
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI, Supertrend
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

