# Intraday-Zeithandel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie eröffnet Long- und/oder Short-Positionen zu einer bestimmten Tageszeit mit vordefinierten Stop-Loss- und Take-Profit-Abständen. Sie ist nützlich, um zeitbasierte Einstiege ohne Indikatorbestätigung zu testen.

## Details

- **Einstiegskriterien**: Zeitbasierter Auslöser zur konfigurierten Stunde und Minute.
- **Long/Short**: Beide Richtungen (konfigurierbar).
- **Ausstiegskriterien**: Schutz-Stop oder Ziel.
- **Stops**: Ja.
- **Standardwerte**:
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `TakeProfitTicks` = 20
  - `StopLossTicks` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Sonstige
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Fest
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
