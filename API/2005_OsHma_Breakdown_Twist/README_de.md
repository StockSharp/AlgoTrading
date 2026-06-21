# OsHMA Ausbruch Twist
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die auf dem OsHMA-Oszillator (Differenz zwischen schnellem und langsamem Hull Moving Average) aufbaut. Sie kann in zwei Modi betrieben werden:

- **Breakdown** – handelt, wenn der Oszillator die Nulllinie kreuzt.
- **Twist** – handelt, wenn der Oszillator seine Richtung ändert.

Die Strategie abonniert Kerzen des ausgewählten Zeitrahmens und verwendet Hull-Moving-Average-Indikatoren zur Berechnung des Oszillators.

## Details

- **Einstiegskriterien**: OsHMA-Nulllinienkreuzung oder Richtungsänderung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensignal oder Stop.
- **Stops**: Take Profit und Stop Loss.
- **Standardwerte**:
  - `FastHma` = 13
  - `SlowHma` = 26
  - `Mode` = Twist
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: H4
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
