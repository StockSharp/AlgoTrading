# Übergeordnete Session Sweep-Alarm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie überwacht tägliche Sessions und erkennt, wenn die aktuelle Session das Hoch oder Tief der vorherigen Session überstreicht. Wenn ein Sweep auftritt und die Kerze zurück in die vorherige Range schließt, wird ein Trade in die entgegengesetzte Richtung mit einem konfigurierbaren Chance-Risiko-Verhältnis eröffnet.

## Details

- **Einstiegskriterien**: Sweep des Hochs/Tiefs der vorherigen Session mit optionalem Kerzenschluss-Filter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop am Session-Extremum oder Ziel basierend auf dem Chance-Risiko-Verhältnis.
- **Stops**: Ja.
- **Standardwerte**:
  - `MinRiskReward` = 1
  - `UseCandleFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Price Action
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
