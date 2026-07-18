# Iron Bot Statistischer Trendfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt Ausbrüche basierend auf statistischen Trendniveaus, die aus Fibonacci-Bereichen und Z-Score berechnet werden.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs kreuzt über Trendlinie und oberes Trendniveau mit nicht-negativem Z-Score.
  - **Short**: Kurs kreuzt unter Trendlinie und unteres Trendniveau mit nicht-positivem Z-Score.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Stop-Loss bei `SlRatio` Prozent vom Einstieg.
  - Take-Profit auf einer von vier Ebenen (`Tp1Ratio`–`Tp4Ratio`) vom Einstieg.
- **Stops**: Ja.
- **Standardwerte**:
  - `ZLength` = 40.
  - `AnalysisWindow` = 44.
  - `HighTrendLimit` = 0.236.
  - `LowTrendLimit` = 0.786.
  - `EmaLength` = 200.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Z-Score, EMA, Kursaktion
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
