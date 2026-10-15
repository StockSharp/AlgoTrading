# AI Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die AI Grid-Strategie platziert gestaffelte Kauf- und Verkaufsorders rund um den aktuellen Preis. Die Strategie unterstützt Ausbruchs- (Stop) und Gegentrend- (Limit) Ansätze. Nach Ausführung einer Order wird automatisch eine Take-Profit-Order platziert.

## Details

- **Einstiegskriterien**: Der Preis erreicht eines der Grid-Levels.
- **Long/Short**: Gesteuert über `AllowLong` und `AllowShort`.
- **Ausstiegskriterien**: Take-Profit nach festem Abstand `TakeProfit`.
- **Stops**: Kein Stop-Loss.
- **Standardwerte**:
  - `GridSize` = 50m
  - `GridSteps` = 10
  - `TakeProfit` = 50m
  - `AllowLong` = true
  - `AllowShort` = true
  - `UseBreakout` = true
  - `UseCounter` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Grid
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nur Take-Profit
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
