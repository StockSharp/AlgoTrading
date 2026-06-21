# RSI Adaptiver T3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolgstrategie basierend auf dem RSI-adaptiven gleitenden Durchschnitt Tillson T3. Geht long, wenn der T3 über seinen Zwei-Balken-Verzögerungswert kreuzt, und steigt beim entgegengesetzten Kreuzung aus.

Backtests auf Tages-Charts zeigen eine stabile Performance in Trendmärkten.

## Details

- **Einstiegskriterien**: T3 kreuzt über seinen Wert von vor 2 Balken.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Entgegengesetzter Kreuzung.
- **Stops**: Nein.
- **Standardwerte**:
  - `RsiLength` = 14
  - `MinT3Length` = 5
  - `MaxT3Length` = 50
  - `VolumeFactor` = 0.7
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: RSI, T3
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
