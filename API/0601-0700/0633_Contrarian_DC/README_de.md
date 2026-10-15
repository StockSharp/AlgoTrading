# Konträre DC-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die konträre DC-Strategie handelt gegen Donchian-Channel-Ausbrüche. Sie kauft, wenn der Kurs das untere Band unterschreitet, und verkauft, wenn der Kurs das obere Band berührt. Nach einem Stop-Loss werden Einstiege in dieselbe Richtung für eine Anzahl von Kerzen pausiert. Das Risikomanagement verwendet symmetrische Stop-Loss- und Take-Profit-Niveaus basierend auf einem Risiko/Rendite-Verhältnis.

## Details
- **Einstiegskriterien**:
  - **Long**: Kurshoch <= Donchian Low && Pause erfüllt
  - **Short**: Kurshoch >= Donchian High && Pause erfüllt
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Stop**: Prozentualer Stop-Loss
  - **Ziel**: Risiko/Rendite-basierter Take-Profit
  - **Band**: Schließen beim Erreichen des gegenüberliegenden Bandes
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `DonchianPeriod` = 20
  - `RiskRewardRatio` = 1.7m
  - `StopLossPercent` = 0.3m
  - `PauseCandles` = 3
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Donchian Channel
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
