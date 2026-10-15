# Labouchere-EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen Stochastik-Oszillator-Kreuzung mit einer Labouchere-Geldmanagement-Sequenz. Der Stochastik-Indikator generiert Signale, wenn %K %D kreuzt. Das Labouchere-System passt das Handelsvolumen nach jeder geschlossenen Position an: Verluste fügen ein neues Element hinzu, das der Summe der ersten und letzten Zahl der Sequenz entspricht, während Gewinne diese Elemente entfernen.

Trades werden nur auf abgeschlossenen Kerzen eingegangen. Die Sequenz kann optional neu gestartet werden, wenn alle Zahlen entfernt wurden. Ein Zeitfilter ermöglicht den Handel innerhalb eines bestimmten Intraday-Fensters, und entgegengesetzte Signale können bestehende Positionen schließen. Feste Stop-Loss- und Take-Profit-Level (in Preisschritten) werden unterstützt.

## Details
- **Einstiegskriterien**:
  - **Long**: %K kreuzt über %D.
  - **Short**: %K kreuzt unter %D.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Optionaler Ausstieg bei entgegengesetztem Signal.
  - Fester Stop-Loss und Take-Profit (falls gesetzt).
- **Stops**: Ja.
- **Geldmanagement**: Labouchere-Sequenz.
- **Standardwerte**:
  - `LotSequence` = "0.01,0.02,0.01,0.02,0.01,0.01,0.01,0.01"
  - `NewRecycle` = true
  - `StopLoss` = 40
  - `TakeProfit` = 50
  - `IsReversed` = false
  - `UseOppositeExit` = false
  - `UseWorkTime` = false
  - `StartTime` = 00:00
  - `StopTime` = 24:00
  - `KPeriod` = 10
  - `DPeriod` = 190
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
