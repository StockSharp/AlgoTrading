# MACD Mean Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Methode verfolgt das MACD-Histogramm im Verhältnis zu seinem eigenen Durchschnitt. Extreme Histogrammwerte kehren oft um, sobald der Schwung nachlässt. Durch die Überwachung der Differenz zwischen MACD und seiner Signallinie findet die Strategie überdehnte Bewegungen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 67%. Er funktioniert am besten auf dem Aktienmarkt.

Eine Long-Position wird eingegangen, wenn das MACD-Histogramm um `DeviationMultiplier` Standardabweichungen unter den Mittelwert fällt. Eine Short-Position wird eröffnet, wenn das Histogramm um denselben Betrag über den Mittelwert steigt. Der Trade wird geschlossen, wenn das Histogramm wieder durch seinen Durchschnitt kreuzt.

Dieser Ansatz richtet sich an Trader, die sich damit wohlfühlen, gegen Momentumextreme zu handeln. Ein Stop-Loss, gemessen als Prozentsatz des Einstiegspreises, schützt gegen Trends, die weiter an Stärke gewinnen.

## Details
- **Einstiegskriterien**:
  - **Long**: MACD Histogram < Avg - DeviationMultiplier * StdDev
  - **Short**: MACD Histogram > Avg + DeviationMultiplier * StdDev
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn Histogram > Avg
  - **Short**: Ausstieg wenn Histogram < Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

