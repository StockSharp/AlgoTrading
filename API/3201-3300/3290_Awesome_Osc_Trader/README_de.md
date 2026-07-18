# Awesome-Osc-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert den MetaTrader-Expert "Awesome Osc Trader", indem sie Bollinger-Bandbreite, einen Stochastic-Filter und eine normalisierte Awesome-Oscillator-Momentumprüfung kombiniert. Long-Trades werden eröffnet, wenn der Oszillator von einem negativen Extrem ansteigt, während Stochastic den überverkauften Bereich verlässt und die Marktvolatilität innerhalb einer konfigurierbaren Bandbreite bleibt. Shorts verlangen die gespiegelten Bedingungen. Ein konfigurierbares Handelsfenster beschränkt neue Orders auf bestimmte Stunden, und offene Positionen können bei Gegensignalen nur dann zwangsweise geschlossen werden, wenn der schwebende Gewinn zum gewählten Filter passt.

## Einzelheiten

- **Einstiegskriterien**:
  - Der Bollinger-Band-Spread, in Pips umgerechnet, muss zwischen `BollingerSpreadLowerLimit` und `BollingerSpreadUpperLimit` bleiben.
  - Die Stochastic-Hauptlinie liegt für Longs über `StochLower` oder für Shorts unter `StochUpper`.
  - Der normalisierte Awesome Oscillator hat mindestens vier aufeinanderfolgende Bars auf der Gegenseite von null gezeigt und dreht mit Stärke über `AoStrengthLimit` zurück in Richtung null.
  - Die aktuelle Zeit liegt innerhalb des durch `EntryHour` und `OpenHours` definierten Handelsfensters.
- **Long/Short**: handelt beide Richtungen.
- **Ausstiegskriterien**:
  - Optionaler früher Ausstieg, wenn ein Gegensignal erscheint oder der Oszillator null kreuzt, gesteuert durch `CloseTrade` und `ProfitTypeClTrd`.
  - Schutz-Stop-Loss-, Take-Profit- und Trailing-Stop-Distanzen in Pips.
- **Stops**: fester Stop, Take-Profit und optionaler Trailing Stop, verwaltet über `StartProtection`.
- **Standardwerte**:
  - `BollingerPeriod` = 20, `BollingerSigma` = 2
  - `BollingerSpreadLowerLimit` = 55, `BollingerSpreadUpperLimit` = 380
  - `PeriodFast` = 3, `PeriodSlow` = 32
  - `AoStrengthLimit` = 0.13
  - `StochK` = 8, `StochD` = 3, `StochSlow` = 3
  - `StochLower` = 18, `StochUpper` = 76
  - `EntryHour` = 0, `OpenHours` = 16
  - `Lots` = 0.01, `TakeProfit` = 200, `StopLoss` = 80, `TrailingStop` = 40
  - `CloseTrade` = true, `ProfitTypeClTrd` = 1 (nur profitable Positionen schließen)
- **Filter**:
  - Kategorie: Momentum mit Volatilitätsfilter
  - Richtung: Long und Short
  - Indikatoren: Bollinger Bands, Stochastic Oscillator, Awesome Oscillator
  - Stops: Ja (fest und trailing)
  - Komplexität: Mittel
  - Zeitrahmen: Für H1 konzipiert, funktioniert aber mit jeder Kerzenserie
  - Saisonalität: Handelsstundenfenster
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
