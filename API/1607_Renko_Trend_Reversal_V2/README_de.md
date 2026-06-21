# Renko Trendumkehr-Strategie V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Renko Trendumkehr-Strategie V2 handelt, wenn der Renko-Eröffnungskurs den Renko-Schlusskurs kreuzt. Sie verwendet ATR-basierte Renko-Steine und setzt Stop-Loss- und Take-Profit-Niveaus. Shorts können deaktiviert werden.

## Details

- **Einstiegskriterien**: Renko Eröffnungs-/Schlusskreuzung mit Zeitfenster
- **Long/Short**: Beide (Shorts optional)
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit
- **Stops**: Ja
- **Standardwerte**:
  - `RenkoAtrLength` = 10
  - `StopLossPct` = 3
  - `TakeProfitPct` = 20
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Renko
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
