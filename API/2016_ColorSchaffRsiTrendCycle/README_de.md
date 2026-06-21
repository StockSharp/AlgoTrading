# Color Schaff RSI Trend-Zyklus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolgesystem basierend auf dem Color Schaff RSI Trend Cycle (STC)-Oszillator. Die Strategie reagiert auf Farbübergänge des STC-Indikators, um Long- und Short-Positionen ein- und auszusteigen.

## Details

- **Einstiegskriterien**:
  - **Long**: Indikatorfarbe vor zwei Kerzen > 5 und letzte Kerze < 6.
  - **Short**: Indikatorfarbe vor zwei Kerzen < 2 und letzte Kerze > 1.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Long-Positionen schließen, wenn Indikatorfarbe vor zwei Kerzen < 2.
  - Short-Positionen schließen, wenn Indikatorfarbe vor zwei Kerzen > 5.
- **Indikatoren**: Color Schaff RSI Trend Cycle.
- **Standardwerte**:
  - `Fast RSI` = 23
  - `Slow RSI` = 50
  - `Cycle` = 10
  - `High Level` = 60
  - `Low Level` = -60
- **Zeitrahmen**: Standardmäßig 4-Stunden-Kerzen.
- **Stops**: Keine.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
