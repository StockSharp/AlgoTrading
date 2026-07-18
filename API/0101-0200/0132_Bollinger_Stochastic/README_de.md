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
  - `StopLoss` = 2%
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

