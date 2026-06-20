# ZScore
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Z-Score-Indikator für Mean-Reversion-Trading

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 121%. Sie funktioniert am besten auf dem Kryptomarkt.

ZScore misst die Preisabweichung von einem gleitenden Durchschnitt. Extreme hohe oder niedrige Z-Scores deuten auf Überextension hin und veranlassen Trades in die entgegengesetzte Richtung. Der Trade endet, wenn sich der Z-Score normalisiert.

Z-Score ist ein flexibler Filter, da er auf jede Zeitreihe skaliert werden kann. Die Verwendung eines volatilitätsangepassten Ausstiegs hilft dem System, sich an sich ändernde Marktbedingungen anzupassen.


## Details

- **Einstiegskriterien**: Signale basierend auf MA, ZScore.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `ZScoreEntryThreshold` = 2.0m
  - `ZScoreExitThreshold` = 0.0m
  - `MAPeriod` = 20
  - `StdDevPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: MA, ZScore
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

