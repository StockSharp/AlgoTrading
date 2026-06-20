# RSI-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die RSI-Ausbruch-Strategie sucht nach Momentum-Schüben, wenn der Relative Strength Index seinen typischen Bereich überschreitet. Durch die Messung von RSI-Abweichungen von seinem gleitenden Durchschnitt zielt das System darauf ab, neue Trends zu erfassen, sobald sie beginnen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 88%. Die Strategie funktioniert am besten am Aktienmarkt.

Eine Long-Position wird eröffnet, wenn der RSI über den Durchschnitt plus `Multiplier` mal die Standardabweichung schließt. Eine Short-Position wird eingegangen, wenn der RSI unter den Durchschnitt minus diesen Multiplikator fällt. Positionen werden geschlossen, sobald der RSI zurück durch seinen Durchschnittswert kreuzt.

Momentum-Trader können diesen Ansatz nützlich finden, um frühe Ausbrüche zu identifizieren und gleichzeitig definierte Ausstiegsniveaus beizubehalten. Ein Stop-Loss-Prozentsatz schützt vor plötzlichen Umkehrungen.

## Details
- **Einstiegskriterien**:
  - **Long**: RSI > Avg + Multiplier * StdDev
  - **Short**: RSI < Avg - Multiplier * StdDev
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn RSI < Avg
  - **Short**: Ausstieg wenn RSI > Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
