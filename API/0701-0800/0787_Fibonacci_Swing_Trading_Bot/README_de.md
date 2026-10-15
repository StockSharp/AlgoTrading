# Fibonacci Swing-Trading-Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Fibonacci-Retracement-Niveaus verwendet, um Swing-Bewegungen zu handeln.

Dieser Bot berechnet die Retracement-Niveaus 0,618 und 0,786 aus dem Bereich der letzten 50 Bars und öffnet Positionen, wenn Kerzen über oder unter diese Niveaus ausbrechen. Das Risikomanagement erfolgt über konfigurierbare Stop-Loss- und Risiko/Rendite-Parameter.

## Details

- **Einstiegskriterien**: Kursaktion mit Fibonacci-Niveaus.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `FiboLevel1` = 0.618
  - `FiboLevel2` = 0.786
  - `RiskRewardRatio` = 2
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Swing
  - Richtung: Beide
  - Indikatoren: Fibonacci, Donchian
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: 4h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

