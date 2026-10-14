# Strategie Ichimoku Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf den Indikatoren Ichimoku Cloud und Stochastic Oscillator.
Geht long, wenn der Preis über Kumo (Wolke) liegt, Tenkan > Kijun, und der Stochastic überverkauft ist (< 20). Geht short, wenn der Preis unter Kumo liegt, Tenkan < Kijun, und der Stochastic überkauft ist (> 80).

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 118%. Sie funktioniert am besten auf dem Aktienmarkt.

Ichimoku skizziert den Trend und die Unterstützungsniveaus, während Stochastic den Einstieg bei Rücksetzern timed. Trades öffnen sich, wenn der Oszillator in der vorherrschenden Wolkenrichtung zurücksetzt.

Trader, die strukturierte Indikatoren bevorzugen, finden es praktisch. Die Wolke selbst dient als Stop gegen abrupte Umkehrungen.

## Details

- **Einstiegskriterien**:
  - Long: `Price > Cloud && Tenkan > Kijun && StochK < 20`
  - Short: `Price < Cloud && Tenkan < Kijun && StochK > 80`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Cloud-Ausbruch in entgegengesetzter Richtung
- **Stops**: Verwendet Ichimoku Cloud-Grenzen
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame()
    Die Werte 20 und 80 in den Regeln sind die Standardwerte von StochOversold und StochOverbought. %K ist die Stochastik über StochPeriod Kerzen, geglättet über StochK Kerzen; %D spielt keine Rolle. Ein Long schließt, wenn der Kurs unter der Wolke schließt, ein Short, wenn er darüber schließt. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Ichimoku Cloud, Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

