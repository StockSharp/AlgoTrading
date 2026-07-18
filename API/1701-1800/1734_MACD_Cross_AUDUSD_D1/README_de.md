# MACD-Kreuzung AUDUSD D1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die AUDUSD auf dem Tages-Zeitrahmen mithilfe von MACD-Linienkreuzungen handelt.

Die Strategie eröffnet eine Long-Position, wenn die MACD-Hauptlinie über die Signallinie kreuzt, und eine Short-Position, wenn sie darunter kreuzt. Der Handel ist nur zwischen 06:00 und 14:00 Uhr Serverzeit erlaubt, und es kann jeweils nur eine Position geöffnet sein. Jeder Trade setzt standardmäßig einen Stop-Loss von 40 Pips und einen dreimal größeren Take-Profit.

## Details

- **Einstiegskriterien**: MACD-Hauptlinie kreuzt die Signallinie.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `Volume` = 0.1
  - `StopLossPips` = 40
  - `RewardRatio` = 3
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
