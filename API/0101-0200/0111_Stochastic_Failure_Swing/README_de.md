# Stochastic Failure Swing Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Stochastic Failure Swing überwacht den Oszillator auf ein niedrigeres Hoch über 80 oder ein höheres Tief unter 20.
Wenn der Indikator keinen neuen Extremwert erreicht und dann umkehrt, signalisiert dies oft einen Trendwechsel.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 70%. Die Strategie funktioniert am besten am Aktienmarkt.

Die Strategie kauft, wenn sich ein höheres Tief unter 20 bildet und %K wieder über %D kreuzt, oder verkauft, wenn ein niedrigeres Hoch über 80 auftritt und %K darunter kreuzt.

Trades verwenden einen kleinen prozentualen Stop und werden geschlossen, wenn der Stochastic durch das vorherige Swing-Niveau zurückkreuzt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Stochastic
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

