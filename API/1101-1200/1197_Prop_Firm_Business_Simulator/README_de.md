# Prop Firm Business Simulator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die das Risikomanagement einer Prop Firm mithilfe von Keltner-Kanal-Ausbrüchen und einer auf dem Risiko pro Trade basierenden Positionsgröße simuliert.

Die Methode platziert Stop-Orders an den Kanalgrenzen. Die Menge wird so berechnet, dass der Abstand zwischen den Bändern dem gewählten Prozentsatz des Kontokapitals entspricht.

## Details

- **Einstiegskriterien**: Der Kurs bricht die Keltner-Kanal-Bänder.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Ausbruch des gegenüberliegenden Bandes.
- **Stops**: Ja.
- **Standardwerte**:
  - `MaPeriod` = 20
  - `AtrPeriod` = 10
  - `Multiplier` = 2m
  - `RiskPerTrade` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keltner, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
