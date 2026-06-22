# ColorMETRO WPR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den ColorMETRO Williams %R-Indikator, der schnelle und langsame Treppenlinien um den Williams %R-Oszillator aufbaut.
Die schnelle Linie reagiert schnell auf Preisänderungen, während die langsame Linie das Rauschen glättet. Handelsentscheidungen werden getroffen, wenn sich diese Linien
kreuzen, was auf potenzielle Impulswechsel hinweist. Wenn die schnelle Linie unter die langsame fällt, geht die Strategie davon aus, dass der
Markt überverkauft ist und eröffnet eine Long-Position. Wenn die schnelle Linie über die langsame steigt, wird eine Short-Position eröffnet.
Bestehende Positionen werden geschlossen, wenn die entgegengesetzte Bedingung erkannt wird.

Das Risikomanagement erfolgt über prozentbasierte Take-Profit- und Stop-Loss-Levels. Dies ermöglicht es der Strategie, sich an die Preisniveaus
verschiedener Instrumente anzupassen. Der Standard-Kerzen-Zeitrahmen beträgt acht Stunden, was hilft, die Intraday-Volatilität herauszufiltern und
sich auf mittelfristige Trends zu konzentrieren. Die Logik funktioniert auf beiden Seiten des Marktes und ermöglicht Long- und Short-Operationen.

## Details

- **Einstiegskriterien**:
  - **Long**: `Schnelle Linie` kreuzt **unter** die `Langsame Linie`.
  - **Short**: `Schnelle Linie` kreuzt **über** die `Langsame Linie`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: `Schnelle Linie` steigt über die `Langsame Linie`.
  - **Short**: `Schnelle Linie` fällt unter die `Langsame Linie`.
- **Stops**: Ja, prozentbasierter Take-Profit und Stop-Loss.
- **Standardwerte**:
  - `WprPeriod` = 7.
  - `FastStep` = 5.
  - `SlowStep` = 15.
  - `TakeProfitPercent` = 4.
  - `StopLossPercent` = 2.
  - `CandleType` = 8-Stunden-Kerzen.
- **Filter**:
  - Kategorie: Trendfolge.
  - Richtung: Beide.
  - Indikatoren: Einzeln (basierend auf Williams %R).
  - Stops: Ja.
  - Komplexität: Mittel.
  - Zeitrahmen: Mittelfristig.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Mittel.
