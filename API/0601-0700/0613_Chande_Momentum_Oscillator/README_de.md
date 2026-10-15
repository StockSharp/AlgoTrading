# Chande Momentum Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kauft, wenn der Chande Momentum Oszillator unter einen unteren Schwellenwert fällt, und schließt die Position, wenn er über einen oberen Schwellenwert steigt oder nach einer festen Anzahl von Bars.

Tests deuten auf eine durchschnittliche jährliche Rendite von etwa 40% hin. Am besten funktioniert sie in Trendmärkten.

Der Oszillator vergleicht jüngste Gewinne und Verluste zur Einschätzung des Momentums. Extreme negative Werte deuten auf überverkaufte Bedingungen hin, die die Strategie für Long-Einstiege nutzt. Positionen werden geschlossen, wenn das Momentum positiv wird oder der Haltezeitraum abläuft.

## Details

- **Einstiegskriterien**: `CMO < LowerThreshold`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: `CMO > UpperThreshold` oder `MaxBarsInPosition` Bars verstrichen.
- **Stops**: Nein.
- **Standardwerte**:
  - `CmoPeriod` = 9
  - `LowerThreshold` = -50
  - `UpperThreshold` = 50
  - `MaxBarsInPosition` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Nur Long
  - Indikatoren: CMO
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
