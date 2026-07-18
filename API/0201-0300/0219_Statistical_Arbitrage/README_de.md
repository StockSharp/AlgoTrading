# Statistische Arbitrage-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser statistische Arbitrage-Ansatz handelt ein Paar verwandter Wertpapiere basierend auf ihrer relativen Positionierung um gleitende Durchschnitte. Durch den Vergleich jedes Vermögenswerts mit seinem eigenen Durchschnitt versucht die Strategie, kurzfristige Disbalancen auszunutzen, die im Laufe der Zeit konvergieren sollten.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 94%. Sie funktioniert am besten am Aktienmarkt.

Eine Long-Position wird eingeleitet, wenn der erste Vermögenswert unter seinem gleitenden Durchschnitt handelt, während der zweite Vermögenswert über seinem eigenen Durchschnitt handelt. Eine Short-Position tritt auf, wenn der erste Vermögenswert über seinem Durchschnitt und der zweite darunter liegt. Positionen werden geschlossen, wenn der erste Vermögenswert wieder durch seinen gleitenden Durchschnitt kreuzt, was signalisiert, dass sich die Spread normalisiert hat.

Die Methode ist ideal für marktneutrale Trader, die mit der Balance von Exponierung über zwei Instrumente vertraut sind. Der eingebaute Stop-Loss begrenzt Drawdowns, wenn sich die Spread weiter ausweitet, anstatt sich umzukehren.

## Details
- **Einstiegskriterien**:
  - **Long**: Asset1 < MA1 && Asset2 > MA2
  - **Short**: Asset1 > MA1 && Asset2 < MA2
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn Asset1 über MA1 schließt
  - **Short**: Ausstieg, wenn Asset1 unter MA1 schließt
- **Stops**: Ja, prozentualer Stop-Loss auf Spread.
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Arbitrage
  - Richtung: Beide
  - Indikatoren: Moving Averages
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
