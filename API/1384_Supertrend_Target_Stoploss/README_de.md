# Supertrend-Strategie mit Ziel und Stop-Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die kauft wenn der Preis die Supertrend-Linie von unten kreuzt, und verkauft wenn er sie von oben kreuzt. Ein festes Prozent-Ziel und Stop-Loss schließen die Positionen.

## Details

- **Einstiegskriterien**: Preis kreuzt den Supertrend.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Ziel- oder Stop-Loss-Prozentsatz.
- **Stops**: Ja, fester Prozentsatz.
- **Standardwerte**:
  - `Period` = 14
  - `Multiplier` = 3m
  - `TargetPct` = 0.01m
  - `StopPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, Supertrend
  - Stops: Fest
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
