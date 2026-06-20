# Delta Neutral Arbitrage-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Arbitrage-Strategie handelt den Spread zwischen zwei korrelierten Assets und hält die kombinierte Position nahe delta-neutral. Durch das Ausbalancieren einer Long-Position in einem Asset gegen eine Short in einem anderen versucht sie, von der Mean Reversion im Spread zu profitieren anstatt von der Marktrichtung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 43%. Er funktioniert am besten auf dem Aktienmarkt.

Ein langer Spread wird eingegangen, wenn der Z-Score der Preisdifferenz unter `-EntryThreshold` fällt. Das erste Asset wird gekauft und das zweite in gleicher Größe verkauft. Ein kurzer Spread macht das Gegenteil, wenn der Z-Score über den positiven Schwellenwert steigt. Der Trade wird geschlossen, sobald der Spread zum gleitenden Durchschnitt zurückkehrt.

Delta-neutrales Trading ist bei quantitativen Tradern beliebt, die ein Engagement mit geringer Volatilität suchen. Obwohl abgesichert, wird dennoch ein Stop-Loss-Schutz angewendet, um gegen extreme Divergenz zwischen den Assets zu schützen.

## Details
- **Einstiegskriterien**:
  - **Long**: Spread Z-Score < -EntryThreshold
  - **Short**: Spread Z-Score > EntryThreshold
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn Spread wieder über den Mittelwert kreuzt
  - **Short**: Ausstieg wenn Spread wieder unter den Mittelwert kreuzt
- **Stops**: Ja, prozentualer Stop-Loss auf den Spread-Wert.
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `EntryThreshold` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Arbitrage
  - Richtung: Beide
  - Indikatoren: Spread statistics
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel

