# IU Eröffnungsbereichs-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die IU Opening Range Breakout Strategie überwacht das Hoch und Tief der ersten Bar jeder Session und handelt Ausbrüche in beide Richtungen. Stops verwenden das Extremum der vorherigen Bar und Ziele werden aus einem konfigurierbaren Risiko-Ertrags-Verhältnis abgeleitet. Alle Positionen werden zur benutzerdefinierten Endzeit geschlossen.

## Details

- **Einstiegskriterien**:
  - Long, wenn der Schluss über das Hoch der ersten Bar kreuzt.
  - Short, wenn der Schluss unter das Tief der ersten Bar kreuzt.
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Stop am Tief/Hoch der vorherigen Bar.
  - Ziel basierend auf dem Risiko-Ertrags-Verhältnis.
  - Alle Positionen bei `EndTime` schließen.
- **Stops**: Ja
- **Standardwerte**:
  - `RiskReward` = 2.0
  - `MaxTrades` = 2
  - `EndTime` = 15:00
  - `CandleType` = 1 Minute
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
