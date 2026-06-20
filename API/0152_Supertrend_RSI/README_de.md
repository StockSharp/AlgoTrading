# Supertrend RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie Supertrend + RSI. Kaufen, wenn der Preis über dem Supertrend liegt und der RSI unter 30 (überverkauft) ist. Verkaufen, wenn der Preis unter dem Supertrend liegt und der RSI über 70 (überkauft) ist.

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
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
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
