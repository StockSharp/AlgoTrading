# Dynamische Unterstützungs- und Widerstands-Pivot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie leitet dynamische Unterstützungs- und Widerstandsniveaus aus jüngsten Pivot-Hochs und -Tiefs ab. Sie geht long, wenn der Preis nahe dem Niveau über die Unterstützung kreuzt, und short, wenn der Preis unter den Widerstand kreuzt. Das Risikomanagement verwendet feste prozentuale Stop-Loss- und Take-Profit-Level.

## Details

- **Einstiegskriterien**: Preis nahe Unterstützung/Widerstand innerhalb von `SupportResistanceDistance` Prozent und Kreuzung über Unterstützung oder unter Widerstand.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Fester Take-Profit und Stop-Loss.
- **Stops**: Ja.
- **Standardwerte**:
  - `PivotLength` = 2
  - `SupportResistanceDistance` = 0.4m
  - `StopLossPercent` = 10.0m
  - `TakeProfitPercent` = 26.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Pivot
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
