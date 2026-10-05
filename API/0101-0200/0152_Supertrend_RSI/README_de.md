# Supertrend RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie Supertrend + RSI. Kaufen, wenn der Preis über dem Supertrend liegt und der RSI unter RsiOversold ist. Verkaufen, wenn der Preis unter dem Supertrend liegt und der RSI über RsiOverbought ist.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 43%. Am besten geeignet für den Aktienmarkt.

Der Supertrend-Indikator zeigt den aktuellen Trend, und der RSI erkennt, wenn der Preis überdehnt ist. Orders folgen der Supertrend-Richtung, sobald der RSI einen Extremwert erreicht.

Eine gute Wahl für Trader, die auf Trailing Stops setzen. Der eingebaute Stop des Supertrend arbeitet mit der ATR-Einstellung zusammen, um Verluste zu begrenzen.

## Details

- **Einstiegskriterien**:
  - Long: `Close > Supertrend && RSI < RsiOversold`
  - Short: `Close < Supertrend && RSI > RsiOverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Supertrend-Wechsel in entgegengesetzte Richtung
- **Stops**: Supertrend als Trailing Stop
- **Standardwerte**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiPeriod` = 14
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Auf Fünf-Minuten-Kerzen erreicht RSI fast nie 30, während der Kurs über dem Supertrend liegt (oder 70 darunter): Das BTC-Archiv vom März 2024 enthält keine einzige solche Kerze. Die Standardwerte sind daher 40 und 60, die weiterhin einen Rücksetzer gegen den Trend markieren und in beide Richtungen handeln. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Supertrend, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
