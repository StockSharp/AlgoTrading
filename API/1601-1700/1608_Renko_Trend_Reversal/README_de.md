# Renko Trendumkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Renko Trendumkehr-Strategie handelt, wenn der Renko-Eröffnungskurs den Renko-Schlusskurs kreuzt. Stop-Loss und Take-Profit können aktiviert werden. Verwendet ATR-basierte Renko-Steine.

## Details

- **Einstiegskriterien**: Renko Eröffnungs-/Schlusskreuzung mit Zeitfenster
- **Long/Short**: Beide
- **Ausstiegskriterien**: optionaler Stop-Loss oder Take-Profit
- **Stops**: Optional
- **Standardwerte**:
  - `RenkoAtrLength` = 10
  - `StopLossPct` = 10
  - `TakeProfitPct` = 50
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Renko
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
