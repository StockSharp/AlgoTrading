# Stochastic Oscillator Mean Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie misst den Stochastic Oscillator gegen seinen eigenen gleitenden Durchschnitt, um überdehnte Schwingungen zu lokalisieren. Wenn %K sich mehrere Standardabweichungen von seinem Mittelwert entfernt, wird erwartet, dass der Indikator zu typischen Werten zurückdriftet.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 64%. Er funktioniert am besten auf dem Devisenmarkt.

Ein Long-Trade wird platziert, wenn Stochastic %K unter das untere Band fällt, das durch den Durchschnitt minus `Multiplier` mal die Standardabweichung definiert wird. Ein Short-Trade erfolgt, wenn %K das obere Band überschreitet. Positionen werden geschlossen, sobald %K wieder durch seine Durchschnittslinie kreuzt.

Die Methode ist für kurzfristige Trader konzipiert, die gerne an überkauften und überverkauften Extremen handeln. Der Stop-Loss schützt vor anhaltendem Momentum, das nicht zur Mean Reversion neigt.

## Details
- **Einstiegskriterien**:
  - **Long**: %K < Avg - Multiplier * StdDev
  - **Short**: %K > Avg + Multiplier * StdDev
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn %K > Avg
  - **Short**: Ausstieg wenn %K < Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

