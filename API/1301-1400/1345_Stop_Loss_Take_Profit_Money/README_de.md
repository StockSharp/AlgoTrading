# Stop-Loss-Take-Profit-Geld-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie geht long, wenn ein kurzfristiger SMA über einen langfristigen SMA kreuzt, und short beim umgekehrten Kreuzungspunkt. Positionen werden geschlossen, sobald Gewinn oder Verlust vordefinierte Geldbeträge erreicht.

## Details

- **Einstiegskriterien**: SMA(14) kreuzt SMA(28)
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gewinn oder Verlust in Geld erreicht das Ziel
- **Stops**: Ja
- **Standardwerte**:
  - `FastLength` = 14
  - `SlowLength` = 28
  - `TakeProfitMoney` = 200
  - `StopLossMoney` = 100
