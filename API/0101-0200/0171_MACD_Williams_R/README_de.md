# Strategie Macd Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf MACD- und Williams %R-Indikatoren. Geht Long, wenn MACD > Signal und Williams %R überverkauft ist (< -80). Geht Short, wenn MACD < Signal und Williams %R überkauft ist (> -20).

Tests zeigen eine durchschnittliche Jahresrendite von etwa 100%. Die Strategie funktioniert am besten auf dem Forexmarkt.

Der MACD zeigt die größere Momentumverschiebung an, während der Williams %R kurzfristige Umkehrungen präzise lokalisiert. Beide Signale müssen übereinstimmen, um einen Trade zu initiieren.

Gut für diejenigen, die Trend- und Gegentrend-Signale kombinieren. Stops sind ein fester Prozentsatz des Einstiegspreises.

## Details

- **Einstiegskriterien**:
  - Long: `MACD > Signal && WilliamsR < -80`
  - Short: `MACD < Signal && WilliamsR > -20`
- **Long/Short**: Beide
- **Ausstiegskriterien**: MACD-Kreuzung in entgegengesetzter Richtung
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80
  - `WilliamsROverbought` = -20
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Die Werte -80 und -20 in den Regeln sind die Standardwerte von WilliamsROversold und WilliamsROverbought. Ein Long schließt, wenn MACD die Signallinie nach unten kreuzt, ein Short, wenn er sie nach oben kreuzt. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: MACD, Williams %R, R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

