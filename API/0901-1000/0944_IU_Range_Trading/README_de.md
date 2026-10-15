# IU Range-Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die IU Range Trading Strategie identifiziert Konsolidierungszonen, in denen die Preisspanne über einen Rückblickzeitraum innerhalb eines ATR-Multiplikators bleibt. Ausbruchstrades werden ausgelöst, wenn der Preis die Bereichsgrenzen überschreitet. Positionen werden durch einen ATR-basierten Trailing-Stop geschützt, der sich mit günstiger Preisentwicklung bewegt.

## Details

- **Einstiegskriterien**: Preis bricht über oder unter eine enge ATR-definierte Range.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-basierter Trailing-Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `RangeLength` = 10
  - `AtrLength` = 14
  - `AtrTargetFactor` = 2.0m
  - `AtrRangeFactor` = 1.75m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR, Highest, Lowest
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
