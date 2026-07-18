# Strategie für gerichtete Bewegungskerzen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie überwacht den Relative Strength Index (RSI) auf Kerzenschlusskursen. Wenn der RSI die neutrale Zone verlässt und benutzerdefinierte Niveaus kreuzt, eröffnet die Strategie Positionen in Richtung des Momentums und schließt jedes entgegengesetzte Exposure.

## Details

- **Indikator**: Relative Strength Index mit einstellbarem `RsiPeriod`.
- **HighLevel**: RSI-Wert, der bullisches Momentum anzeigt.
- **MiddleLevel**: Neutralschwelle als Referenz.
- **LowLevel**: RSI-Wert, der bearisches Momentum anzeigt.
- **Einstieg**:
  - Long wenn RSI über `HighLevel` steigt, nachdem er darunter war.
  - Short wenn RSI unter `LowLevel` fällt, nachdem er darüber war.
- **Ausstieg**: Das entgegengesetzte Signal schließt die bestehende Position, bevor eine neue eröffnet wird.
- **Long/Short**: Beide Richtungen.
- **Stops**: Standardmäßig nicht verwendet.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `HighLevel` = 70
  - `MiddleLevel` = 50
  - `LowLevel` = 30
  - `CandleType` = 5-Minuten-Zeitrahmen
