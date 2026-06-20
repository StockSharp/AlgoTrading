# TradingView Supertrend Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf Supertrend-Indikator-Flips mit Volumenbestätigung

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 79%. Am besten funktioniert sie auf dem Aktienmarkt.

TradingView Supertrend Flip emuliert die Farbwechsel des beliebten Indikators. Ein Wechsel von Rot zu Grün signalisiert einen Long-Einstieg und Grün zu Rot einen Short-Einstieg. Die Strategie steigt beim nächsten Flip aus.

Volumenbestätigung kann verwendet werden, um Fehlsignale in dünn gehandelten Perioden zu vermeiden. Indem nur bei Flips mit unterstützendem Volumen gehandelt wird, zielt die Methode auf zuverlässigere Umkehrungen ab.


## Details

- **Einstiegskriterien**: Signale basierend auf ATR, Supertrend.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `VolumeAvgPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, Supertrend
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neural Networks: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

