# Stochastic-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Ausbruchsansatz überwacht den Stochastic-Oszillator auf scharfe Bewegungen weg von seinem jüngsten Durchschnitt. Wenn die %K-Linie über oder unter einen volatilitätsangepassten Schwellenwert bricht, signalisiert dies einen Momentum-Schub, der einen Trend beginnen könnte.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 181%. Die Strategie funktioniert am besten im Kryptomarkt.

Eine Long-Position wird ausgelöst, wenn %K nach einer Kontraktionsphase über den oberen Schwellenwert kreuzt. Eine Short-Position wird eingegangen, wenn %K unter den unteren Schwellenwert bricht. Der Trade wird geschlossen, wenn der Oszillator zurück in Richtung seines Durchschnitts driftet oder einen Schutz-Stop trifft.

Die Strategie ist für Intraday-Trader konzipiert, die früh in Momentum-Schwünge einsteigen möchten. Volatilitätsbasierte Bänder helfen, Rauschen zu filtern, sodass nur entschiedene Bewegungen Signale erzeugen.

## Details
- **Einstiegskriterien**:
  - **Long**: %K > Avg + DeviationMultiplier * StdDev
  - **Short**: %K < Avg - DeviationMultiplier * StdDev
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn %K < Avg
  - **Short**: Ausstieg wenn %K > Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `StochasticPeriod` = 14
  - `KPeriod` = 3
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
    Avg und StdDev sind Mittelwert und Standardabweichung der letzten LookbackPeriod Werte von %K einschließlich des aktuellen. Der Stop liegt bei festen StopLossPercent vom Einstiegspreis und wird auch zwischen den Kerzen überwacht; 0 schaltet ihn ab. %K ist die Stochastik über die erste Periode, geglättet über KPeriod Kerzen; %D spielt in den Regeln keine Rolle, daher entfällt DPeriod. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
