# Ausbruch-Strategie mit Angebot, Nachfrage und Order Block
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruch-Strategie, die Donchian-Unterstützungs- und Widerstandsniveaus mit EMA-Trendfilter und Volumenspitzen-Bestätigung verwendet. Positionen werden durch Stop-Loss und Trailing-Stop geschützt.

## Details

- **Einstiegskriterien**: Ausbruch aus dem Donchian-Kanal mit Trend- und Volumenfilter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Trailing-Stop.
- **Stops**: Ja, fest und Trailing.
- **Standardwerte**:
  - `Length` = 20
  - `StopLossTicks` = 1000
  - `TrailingStartTicks` = 2000
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Donchian, EMA, SMA
  - Stops: Fest & Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
