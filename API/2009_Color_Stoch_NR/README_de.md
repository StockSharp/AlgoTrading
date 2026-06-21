# Color Stochastic NR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt mit einem Stochastic-Oszillator in mehreren wählbaren Modi. Jeder Modus definiert, wie die %K- und %D-Linien zur Erzeugung von Kauf- und Verkaufssignalen interpretiert werden.

Modi:

- **Breakdown** – Long, wenn %K das Niveau 50 von unten kreuzt; Short, wenn es darunter fällt.
- **OscTwist** – reagiert auf Richtungsänderungen von %K.
- **SignalTwist** – reagiert auf Richtungsänderungen von %D.
- **OscDisposition** – Long, wenn %K %D von unten kreuzt; Short, wenn es %D von oben kreuzt.
- **SignalBreakdown** – handelt, wenn %D das Niveau 50 kreuzt.

Entgegengesetzte Signale schließen bestehende Positionen und eröffnen neue in der entgegengesetzten Richtung. Die Risikosteuerung erfolgt über feste prozentuale Stop-Loss- und Take-Profit-Niveaus.

## Details

- **Einstiegskriterien**:
  - **Long**: Abhängig vom gewählten Modus, siehe oben.
  - **Short**: Abhängig vom gewählten Modus, siehe oben.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop-Schutz.
- **Stops**: Ja, `StopLossPercent` und `TakeProfitPercent`.
- **Standardwerte**:
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Mode` = `OscDisposition`
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 2
  - `CandleType` = 4 hour
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Stochastic
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: 4H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
