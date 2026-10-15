# Zeit-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zeitbasierte Strategie, die genau zu einer bestimmten Uhrzeit eine Long- und/oder Short-Position eröffnet und diese mit konfigurierbarem Take-Profit und Stop-Loss schützt.

## Details

- **Einstiegskriterien**: Um `TradeHour:TradeMinute:TradeSecond` Long öffnen, wenn `AllowBuy`; Short öffnen, wenn `AllowSell`.
- **Long/Short**: Beide, abhängig von den Einstellungen
- **Ausstiegskriterien**: Position wird über Stop-Loss oder Take-Profit geschlossen
- **Stops**: Ja, beide
- **Standardwerte**:
  - `Volume` = 1
  - `TakeProfit` = 0.2
  - `StopLoss` = 0.2
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `TradeSecond` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `CandleType` = TimeSpan.FromSeconds(1).TimeFrame()
- **Filter**:
  - Kategorie: Zeit
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

