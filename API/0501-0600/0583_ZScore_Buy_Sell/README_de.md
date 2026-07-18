# ZScore-Kauf/Verkauf-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet den Z-Score, um extreme Abweichungen vom gleitenden Durchschnitt zu erkennen.
Eine Position wird eröffnet, wenn der Z-Score einen Schwellenwert nach oben oder unten kreuzt, und eine Abkühlphase verhindert wiederholte Signale.

## Details

- **Einstiegskriterien**:
  - Short, wenn Z-Score > `ZThreshold` und die Verkaufs-Abkühlzeit abgelaufen ist.
  - Long, wenn Z-Score < -`ZThreshold` und die Kauf-Abkühlzeit abgelaufen ist.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gegensätzliches Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: SMA, StandardDeviation, Z-Score
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
