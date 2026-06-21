# Smart Money Pivot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche aus Pivot-Hochs und -Tiefs. Eine Long-Position wird eröffnet, wenn der Preis über das letzte Pivot-Hoch steigt, während eine Short-Position eröffnet wird, wenn der Preis unter das letzte Pivot-Tief fällt. Jeder Trade verwendet eigene Stop-Loss- und Take-Profit-Prozentsätze.

## Details

- **Einstiegskriterien**: Ausbruch über Pivot-Hoch oder unter Pivot-Tief.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `EnableLongStrategy` = true
  - `LongStopLossPercent` = 1m
  - `LongTakeProfitPercent` = 1.5m
  - `EnableShortStrategy` = true
  - `ShortStopLossPercent` = 1m
  - `ShortTakeProfitPercent` = 1.5m
  - `Period` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Price Action
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
