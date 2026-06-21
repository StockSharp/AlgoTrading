# Heiken Ashi Supertrend ATR-SL-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Heikin Ashi-Kerzen mit einem Supertrend-Richtungsfilter kombiniert. Einstiege erfordern Kerzen ohne Dochte und können einen ATR-basierten Stop-Loss sowie Break Even aktivieren.

## Details

- **Einstiegskriterien**:
  - Long: grüne HA-Kerze ohne unteren Docht, optionaler Aufwärtstrend-Filter
  - Short: rote HA-Kerze ohne oberen Docht, optionaler Abwärtstrend-Filter
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: rote HA-Kerze ohne oberen Docht oder Stop getroffen
  - Short: grüne HA-Kerze ohne unteren Docht oder Stop getroffen
- **Stops**: ATR-basiert mit optionalem Break Even
- **Standardwerte**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `AtrFactor` = 3m
  - `UseBreakEven` = false
  - `BreakEvenAtrMultiplier` = 1m
  - `UseHardStop` = false
  - `StopLossAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Heikin Ashi, Supertrend, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
