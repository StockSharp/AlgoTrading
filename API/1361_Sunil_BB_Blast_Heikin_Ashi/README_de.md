# Sunil BB Blast Heikin Ashi Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert Bollinger-Bands-Ausbruch mit Heikin Ashi Kerzenbestätigung.

Die Strategie wartet auf einen Bollinger-Band-Ausbruch in Richtung der vorherigen Heikin Ashi- und Standardkerze. Positionen verwenden das gegenüberliegende Band als Stop und ein auf dem Risiko-Ertrags-Verhältnis basierendes Ziel.

## Details

- **Einstiegskriterien**: Preis durchbricht Bollinger Bands bei vorheriger Heikin Ashi und Kerze in gleicher Richtung.
- **Long/Short**: Konfigurierbar über `Direction`.
- **Ausstiegskriterien**: Gewinnmitnahme oder Stop-Loss basierend auf den Bändern.
- **Stops**: Bollinger-Band und Risiko/Ertrags-Verhältnis.
- **Standardwerte**:
  - `BollingerPeriod` = 19
  - `BollingerMultiplier` = 2m
  - `RiskRewardRatio` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `Direction` = TradeDirection.Both
  - `SessionBegin` = 09:20:00
  - `SessionEnd` = 15:00:00
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Bollinger, HeikinAshi
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
