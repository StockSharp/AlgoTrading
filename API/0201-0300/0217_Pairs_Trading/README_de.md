# Strategie Pairs
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Pairs-Trading-Strategie überwacht den Kurs-Spread zwischen zwei korrelierten Instrumenten. Durch den Vergleich des Spreads mit seinem historischen Mittelwert und seiner Standardabweichung versucht das System, vorübergehende Divergenzen auszunutzen, die schließlich revertieren.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 88%. Sie funktioniert am besten auf dem Aktienmarkt.

Ein Long-Spread wird eingegangen, wenn der Spread mehr als den angegebenen Abweichungsmultiplikator unter seinen Mittelwert fällt. Das bedeutet, das erste Asset zu kaufen und das zweite zu verkaufen. Ein Short-Spread macht das Gegenteil, wenn der Spread um den gleichen Betrag über den Mittelwert steigt. Positionen werden geschlossen, sobald der Spread zum Durchschnittsniveau zurückkehrt.

Pairs Trading spricht marktneutrale Trader an, die Relative-Value-Chancen gegenüber direktionalen Trades bevorzugen. Da beide Beine abgesichert sind, ist die Volatilität tendenziell geringer, obwohl die Strategie weiterhin einen Stop-Loss auf dem Spread verwendet, um das Risiko zu managen.

## Details
- **Einstiegskriterien**:
  - **Long**: Spread < Mean - Multiplier * StdDev
  - **Short**: Spread > Mean + Multiplier * StdDev
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn der Spread zum Mittelwert revertiert
  - **Short**: Ausstieg, wenn der Spread zum Mittelwert revertiert
- **Stops**: Ja, prozentualer Stop basierend auf dem Spread-Wert.
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
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

