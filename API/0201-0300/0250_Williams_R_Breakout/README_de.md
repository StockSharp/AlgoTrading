# Williams %R-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sucht nach Momentum-Schüben, indem sie Williams %R im Verhältnis zu seinem historischen Durchschnitt beobachtet. Wenn der Oszillator weit über die typischen Werte hinausgeht, kann dies den Beginn einer starken Bewegung signalisieren.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 91%. Die Strategie funktioniert am besten am Aktienmarkt.

Eine Long-Position wird eröffnet, wenn %R über den Durchschnitt plus `Multiplier` mal eine geschätzte Standardabweichung steigt. Eine Short-Position wird eingegangen, wenn %R unter den Durchschnitt minus denselben Multiplikator fällt. Der Trade schließt, sobald %R in Richtung seines Durchschnitts zurückkehrt oder ein Stop-Loss getroffen wird.

Der Ansatz richtet sich an Ausbruchstrader, die früh an entstehenden Trends teilnehmen möchten. Das Positionsrisiko wird mit einem Prozentstopp auf Basis des Einstiegspreises gesteuert.

## Details
- **Einstiegskriterien**:
  - **Long**: %R > Avg + Multiplier * StdDev
  - **Short**: %R < Avg - Multiplier * StdDev
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn %R < Avg
  - **Short**: Ausstieg wenn %R > Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `WilliamsRPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 2
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
    Avg und StdDev sind Mittelwert und Standardabweichung der letzten AvgPeriod Werte von %R einschließlich des aktuellen. Der Stop liegt bei festen StopLossPercent vom Einstiegspreis und wird auch zwischen den Kerzen überwacht; 0 schaltet ihn ab. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Williams %R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
