# IU Größer-als-Range-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchsstrategie, die Trades eröffnet, wenn der Kerzenkörper größer als die vorherige Handelsspanne der letzten Kerzen ist.

Das System vergleicht den aktuellen Kerzenkörper mit der Spanne zwischen dem höchsten und niedrigsten Eröffnungs-/Schlusskurs über einen konfigurierbaren Rückblickzeitraum. Überschreitet der Körper die vorherige Spanne, wird in Kerzenkörper-Richtung eingestiegen und das Risiko über konfigurierbare Stop-Methoden verwaltet.

## Details

- **Einstiegskriterien**: Kerzenkörper größer als vorherige Spanne; Richtung basiert auf dem Kerzenkörper.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Vorherige Kerze, ATR oder Swing-Niveaus.
- **Standardwerte**:
  - `LookbackPeriod` = 22
  - `RiskToReward` = 3
  - `StopLossMethod` = PreviousHighLow
  - `AtrLength` = 14
  - `AtrFactor` = 2m
  - `SwingLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Highest, Lowest, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
