# MACD-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem MACD-Crossover innerhalb einer festgelegten Zone.

Die MACD-Crossover-Strategie wartet darauf, dass die MACD-Linie die Signallinie kreuzt, während der MACD-Wert zwischen dem unteren und oberen Schwellenwert bleibt. Der entgegengesetzte Crossover schließt die bestehende Position. Es wird kein Stop-Loss angewendet.

## Details

- **Einstiegskriterien**: MACD-Crossover innerhalb der Zone.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Crossover.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `LowerThreshold` = -0.5m
  - `UpperThreshold` = 0.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
