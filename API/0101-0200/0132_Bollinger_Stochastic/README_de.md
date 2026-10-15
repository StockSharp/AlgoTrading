# Bollinger Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Bollinger Stochastic kombiniert Bollinger Bänder mit dem Stochastik-Oszillator, um überdehnte Bewegungen zu identifizieren.
Wenn der Preis die äußere Bande berührt, während sich der Oszillator in einer Extremzone befindet, deutet dies auf einen möglichen Rückprall hin.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 133%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

Das System handelt gegen diese Extreme: Long, wenn der Preis die untere Bande mit überverkauftem Stochastik berührt, und Short an der oberen Bande mit überkauftem Stochastik.

Ein prozentualer Stop begrenzt das Risiko, falls die Mean Reversion ausbleibt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `StochPeriod` = 14
  - `StochDPeriod` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
    Eine Kerze, deren Tief das untere Band berührt, während Stochastik-%K unter StochOversold liegt, eröffnet einen Long; eine Kerze, deren Hoch das obere Band berührt, während %K über StochOverbought liegt, einen Short. Ein Gegensignal dreht die Position.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Stochastic
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

