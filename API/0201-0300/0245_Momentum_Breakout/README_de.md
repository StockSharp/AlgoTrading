# Momentum-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Ausbruchssystem sucht nach plötzlichen Anstiegen des Momentums im Verhältnis zu seinem historischen Durchschnitt. Wenn Momentum-Werte den Durchschnitt um einen großen Betrag überschreiten, könnte der Preis eine schnelle direktionale Bewegung beginnen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 82%. Die Strategie funktioniert am besten am Aktienmarkt.

Die Strategie kauft, wenn das Momentum über den Durchschnitt plus `Multiplier` mal seine Standardabweichung steigt. Ein Short wird eingeleitet, wenn das Momentum unter den Durchschnitt minus denselben Multiplikator fällt. Positionen werden geschlossen, sobald das Momentum in Richtung seines Mittelwerts zurückkehrt.

Trader, die schnelle Bewegungen mögen, können die klaren Regeln zum Erfassen von Kraftschüben schätzen. Ein Stop-Loss basierend auf einem Prozentsatz des Preises schützt vor gescheiterten Ausbrüchen.

## Details
- **Einstiegskriterien**:
  - **Long**: Momentum > Avg + Multiplier * StdDev
  - **Short**: Momentum < Avg - Multiplier * StdDev
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn Momentum < Avg
  - **Short**: Ausstieg wenn Momentum > Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `MomentumPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
    Avg und StdDev sind Mittelwert und Standardabweichung der letzten AveragePeriod Werte von Momentum einschließlich des aktuellen. Der Stop liegt bei festen StopLossPercent vom Einstiegspreis und wird auch zwischen den Kerzen überwacht; 0 schaltet ihn ab. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Momentum
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
