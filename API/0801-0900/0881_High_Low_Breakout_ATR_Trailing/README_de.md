# Hoch-Tief-Ausbruch ATR-Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche aus der ersten 30-Minuten-Sitzungsspanne. Sobald der Preis das anfängliche Hoch oder Tief überschreitet, wird eine Position mit einem ATR-basierten Trailing-Stop eröffnet. Alle Positionen werden zu einer bestimmten Intraday-Zeit geschlossen.

## Details
- **Einstiegskriterien**:
  - **Long**: Schlusskurs überschreitet das 30-Minuten-Hoch
  - **Short**: Schlusskurs unterschreitet das 30-Minuten-Tief
- **Long/Short**: Konfigurierbar (`Direction`).
- **Ausstiegskriterien**:
  - ATR-Trailing-Stop oder symmetrisches Ziel
  - Alle Positionen bei `ExitHour:ExitMinute` schließen
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.5m
  - `RiskPerTrade` = 2m
  - `AccountSize` = 10000m
  - `SessionStartHour` = 9
  - `SessionStartMinute` = 15
  - `ExitHour` = 15
  - `ExitMinute` = 15
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Konfigurierbar
  - Indikatoren: ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
