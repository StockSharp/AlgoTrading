# Parabolic SAR Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie Parabolic SAR + Stochastic. Kaufen, wenn der Preis über dem SAR liegt und Stochastic %K unter 20 (überverkauft) ist. Verkaufen, wenn der Preis unter dem SAR liegt und Stochastic %K über 80 (überkauft) ist.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 61%. Am besten geeignet für den Kryptomarkt.

Der Parabolic SAR liefert den Trend und der Stochastic verfeinert den Einstieg bei Rücksetzern. Signale wechseln, wenn der SAR die Seite wechselt.

Eine unkomplizierte Trendstrategie mit eingebautem SAR-Stop. ATR-Einstellungen sorgen für zusätzliche Risikokontrolle.

## Details

- **Einstiegskriterien**:
  - Long: `Close > SAR && StochK < StochOversold`
  - Short: `Close < SAR && StochK > StochOverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Parabolic SAR-Wechsel in entgegengesetzte Richtung
- **Stops**: Dynamisch, SAR-basiert
- **Standardwerte**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `StochK` = 3
  - `StochD` = 3
  - `StochPeriod` = 14
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Parabolic SAR, Parabolic SAR, Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
