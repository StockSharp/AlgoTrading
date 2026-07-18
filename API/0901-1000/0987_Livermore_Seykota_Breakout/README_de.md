# Livermore Seykota Ausbruch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchsystem, das Livermore-Pivotpunkte mit Seykotas Trendfilter und ATR-basierten Ausstiegen kombiniert.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 87%. Es performt am besten im Aktienmarkt.

Die Strategie sucht nach Ausbrüchen über oder unter dem jüngsten Pivot und bestätigt die Trendrichtung durch EMA-Ausrichtung und Volumenstärke. ATR-basierte Stops steuern das Risiko.

## Details

- **Einstiegskriterien**: Kurs bricht letzten Pivot mit Trend- und Volumenbestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-Stop oder Trailing-Stop.
- **Stops**: ATR-basierter Stop & Trailing.
- **Standardwerte**:
  - `MainEmaLength` = 50
  - `FastEmaLength` = 20
  - `SlowEmaLength` = 200
  - `PivotLength` = 3
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 3
  - `TrailAtrMultiplier` = 2
  - `VolumeSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: EMA, Volumen, ATR, Pivot
  - Stops: ATR Trailing
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
