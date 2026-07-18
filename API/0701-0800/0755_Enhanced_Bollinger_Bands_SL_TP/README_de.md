# Verbesserte Bollinger-Bands-SL-TP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Bollinger-Band-Rückpraller mit Limit-Orders und festem pip-basiertem Stop-Loss und Take-Profit handelt.

## Details

- **Einstiegskriterien**:
  - Long: vorheriger Schlusskurs <= vorherige untere Band und Schlusskurs > untere Band
  - Short: vorheriger Schlusskurs >= vorherige obere Band und Schlusskurs < obere Band
- **Long/Short**: Beide
- **Stops**: Absoluter Take-Profit und Stop-Loss in Pips
- **Standardwerte**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2m
  - `EnableLong` = true
  - `EnableShort` = true
  - `PipValue` = 0.0001m
  - `StopLossPips` = 10m
  - `TakeProfitPips` = 20m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Bollinger Bands
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
