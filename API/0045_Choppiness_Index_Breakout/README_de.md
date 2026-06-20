# Choppiness Index Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der Choppiness Index misst, ob der Markt im Trend oder in einer Seitwärtsbewegung ist. Wenn der Indikator unter einen Schwellenwert fällt, signalisiert er den Beginn eines Trends aus einem choppy Umfeld.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 172%. Am besten funktioniert es im Devisenmarkt.

Diese Strategie tritt in die Richtung des Preises relativ zu seinem gleitenden Durchschnitt ein, wenn die Choppiness sinkt. Sie verlässt die Position, wenn die Choppiness wieder über einen hohen Schwellenwert steigt oder ein Stop-Loss ausgelöst wird.

Das Ziel ist, neue Trends zu erfassen, die nach Konsolidierungsphasen entstehen.

## Details

- **Einstiegskriterien**: Choppiness unter `ChoppinessThreshold` mit Preis über/unter MA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Choppiness über `HighChoppinessThreshold` oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `ChoppinessPeriod` = 14
  - `ChoppinessThreshold` = 38.2m
  - `HighChoppinessThreshold` = 61.8m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Choppiness, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

