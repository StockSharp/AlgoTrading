# ColorNonLagDot MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den MACD-Indikator mit mehreren Signalerkennungsmodi verwendet. Der Ansatz wurde vom MQL-Expert-Advisor "Exp_ColorNonLagDotMACD" portiert.

## Details

- **Einstiegskriterien**: Abhängig vom gewählten Modus (Nulllinien-Ausbruch, MACD-Wende, Signallinien-Wende oder MACD-Kreuzung der Signallinie).
- **Long/Short**: Beide Richtungen, können separat aktiviert werden.
- **Ausstiegskriterien**: Entgegengesetzte Signale oder konfigurierter Stop/Ziel.
- **Stops**: Optionaler prozentualer Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Mode` = `MacdDisposition`
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: 4H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
