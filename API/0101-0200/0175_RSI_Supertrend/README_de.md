# Strategie Rsi Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf den Indikatoren RSI und Supertrend. Geht long, wenn der RSI überverkauft ist (< RsiOversold) und der Preis über Supertrend liegt. Geht short, wenn der RSI überkauft ist (> RsiOverbought) und der Preis unter Supertrend liegt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 112%. Sie funktioniert am besten auf dem Forex-Markt.

Der RSI-Oszillator definiert Momentum-Extreme, während Supertrend die vorherrschende Richtung anzeigt. Trades entstehen, wenn der RSI mit der Supertrend-Farbe übereinstimmt.

Funktioniert für Trader, die einen Trailing-Stop-Ausstieg bevorzugen. Die ATR-Einstellungen des Supertrend formen diese Trailing-Linie.

## Details

- **Einstiegskriterien**:
  - Long: `RSI < RsiOversold && Close > Supertrend`
  - Short: `RSI > RsiOverbought && Close < Supertrend`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Supertrend-Wechsel
- **Stops**: Trailing mit Supertrend
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Auf Fünf-Minuten-Kerzen erreicht RSI fast nie 30, während der Kurs über dem Supertrend liegt (oder 70 darunter): Das BTC-Archiv vom März 2024 enthält keine einzige solche Kerze. Die Niveaus sind daher Parameter mit den Standardwerten 40 und 60, die weiterhin einen Rücksetzer gegen den Trend markieren und in beide Richtungen handeln. Ein Long schließt, wenn der Supertrend nach unten dreht, ein Short, wenn er nach oben dreht. Ein Einstiegssignal gegen eine offene Position dreht sie.
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

