# RSI Nur-Long-Strategie mit bestätigten Rückkreuzungen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wartet darauf, dass der RSI unter einen Schwellenwert fällt und ihn dann wieder nach oben kreuzt. Die Rückkreuzung bestätigt überverkaufte Bedingungen, bevor eine Long-Position eingegangen wird. Positionen werden geschlossen, wenn der RSI ein Ausstiegsniveau nach oben kreuzt. Parameter erlauben Short-Trades, aber die Standardwerte deaktivieren Shorts effektiv.

## Details

- **Einstiegskriterien**: RSI kreuzt das Überverkauft-Niveau nach oben, nachdem er darunter war.
- **Long/Short**: Standardmäßig nur Long.
- **Ausstiegskriterien**: RSI kreuzt das Long-Ausstiegsniveau nach oben oder optionale Short-Regeln greifen.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `RsiLength` = 14
  - `Oversold` = 44
  - `LongExitLevel` = 70
  - `ShortEntryLevel` = 100
  - `ShortExitLevel` = 0
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Long
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
