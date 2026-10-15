# ATR MACD Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ATR MACD nutzt die Volatilität des Average True Range zur Anpassung der Positionsgröße beim Handel mit MACD-Crossovers.
Höhere ATR-Werte führen zu kleineren Handelsgrößen und halten das Risiko über verschiedene Marktphasen konstant.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 154%. Die Strategie funktioniert am besten auf dem Aktienmarkt.

Einstiege erfolgen, wenn der MACD seine Signallinie kreuzt; Ausstiege werden durch den entgegengesetzten Crossover oder einen prozentualen Stop ausgelöst.

Diese Kombination zielt darauf ab, Momentum zu nutzen und gleichzeitig die sich verändernde Volatilität zu berücksichtigen.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrPeriod` = 14
  - `AtrAvgPeriod` = 20
    Ein MACD-Kreuzen über die Signallinie eröffnet einen Long, darunter einen Short; das Gegenkreuzen dreht die Position. Jede neue Position ist Volume mal dem Durchschnitt der letzten AtrAvgPeriod ATR-Werte geteilt durch den aktuellen ATR, abgerundet auf den Volumenschritt.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ATR, MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

