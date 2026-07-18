# Xmacd-Modi-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem MACD-Indikator, die vier verschiedene Einstiegsmodi unterstützt:

- **Breakdown**: Trades öffnen, wenn MACD die Nulllinie kreuzt.
- **MacdTwist**: Reagieren auf einen Richtungswechsel des MACD von fallend zu steigend oder umgekehrt.
- **SignalTwist**: Wendepunkte der Signallinie als Auslöser verwenden.
- **MacdDisposition**: Handel auf Kreuzungen zwischen MACD und seiner Signallinie.

Die Strategie abonniert 4-Stunden-Kerzen und berechnet einen klassischen MACD (EMA 12/26 mit einem 9-Perioden-Signal). Sie kann bei entgegengesetzten Signalen sowohl Positionen eröffnen als auch schließen. Das Risiko wird durch optionalen Stop-Loss und Take-Profit in Prozent des Einstiegspreises gesteuert.

## Details

- **Einstiegskriterien**: MACD-basierte Signale je nach ausgewähltem Modus.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
  - `Mode` = MacdDisposition
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Swing (4h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
