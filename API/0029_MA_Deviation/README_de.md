# MA Deviation
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die handelt, wenn der Preis erheblich von seinem gleitenden Durchschnitt abweicht

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 124%. Sie funktioniert am besten auf dem Forexmarkt.

MA Deviation steigt ein, wenn der Preis einen festgelegten Prozentsatz von seinem gleitenden Durchschnitt abweicht, und erwartet eine Rückkehr zum Mittelwert. Die Position wird geschlossen, wenn der Preis wieder in Richtung des Durchschnitts konvergiert.

Abweichungsschwellenwerte können je nach Volatilität erweitert oder verengt werden. Die Verwendung von ATR für die Positionsgröße hält das Risiko auf allen Märkten konsistent.


## Details

- **Einstiegskriterien**: Signale basierend auf MA, ATR.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `DeviationPercent` = 5m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

