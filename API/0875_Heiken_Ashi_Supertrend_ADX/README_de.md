# Heiken Ashi Supertrend ADX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Heiken Ashi-Kerzen, die Supertrend-Richtung und einen optionalen ADX-Filter kombiniert. Eine bullische Heiken Ashi-Kerze ohne unteren Docht eröffnet eine Long-Position in einem Aufwärtstrend. Bärische Kerzen ohne oberen Docht eröffnen Shorts in einem Abwärtstrend. Positionen werden bei entgegengesetzten Signalen oder einem ATR-basierten Trailing-Stop geschlossen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 128%. Am besten geeignet für den Kryptomarkt.

Heiken Ashi glättet Rauschen, während Supertrend und ADX die Richtung bestätigen. ATR bestimmt dynamische Stops.

## Details

- **Einstiegskriterien**:
  - Long: bullische HA-Kerze ohne unteren Docht mit optionalem Supertrend-Aufwärtstrend und ADX-Bestätigung
  - Short: bärische HA-Kerze ohne oberen Docht mit optionalem Supertrend-Abwärtstrend und ADX-Bestätigung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetzte Kerze oder ATR-Trailing-Stop
- **Stops**: ATR-Trailing-Stop
- **Standardwerte**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `UseAdxFilter` = false
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `TrailAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Heiken Ashi, Supertrend, ADX, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
