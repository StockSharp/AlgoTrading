# Momentum Alligator 4h Bitcoin-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Momentum Alligator 4h Bitcoin-Strategie kombiniert den Awesome Oscillator mit dem Bill Williams Alligator im Tageszeitrahmen. Eine Long-Position wird eröffnet, wenn der Oszillator seine 5-Perioden-SMA von unten kreuzt und der Preis oberhalb aller drei täglichen Alligator-Linien handelt. Ein dynamischer Stop-Loss verwendet den höheren Wert aus dem prozentualen Rückgang vom Einstieg und der Alligator-Kiefer-Linie. Nach einem profitablen Ausstieg überspringt die Strategie die nächsten zwei Signale.

## Details

- **Einstiegskriterien**: AO kreuzt seine 5-Perioden-SMA von unten und der Schlusskurs liegt über den täglichen Alligator-Linien.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Dynamischer Stop-Loss beim Maximum aus prozentualem Stop und Alligator-Kiefer.
- **Stops**: Ja.
- **Standardwerte**:
  - `StopLossPercent` = 0.02m
  - `CandleType` = TimeSpan.FromHours(4)
  - `TradeStart` = 2023-01-01
  - `TradeStop` = 2025-01-01
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Nur Long
  - Indikatoren: Awesome Oscillator, Alligator
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
