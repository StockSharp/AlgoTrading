# Strategie zur Kreuzung gleitender Durchschnitte
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie beobachtet das Verhältnis zwischen einem schnellen und einem langsamen exponentiellen gleitenden Durchschnitt. Eine bullische Kreuzung eröffnet oder dreht in eine Long-Position, eine bärische Kreuzung eröffnet oder dreht in eine Short-Position. Signale werden nur auf abgeschlossenen Kerzen ausgewertet.

## Details

- **Long-Einstieg**: Der schnelle EMA kreuzt den langsamen EMA von unten nach oben.
- **Short-Einstieg**: Der schnelle EMA kreuzt den langsamen EMA von oben nach unten.
- **Ausstieg**: Eine entgegengesetzte Kreuzung dreht die Position; ein prozentualer Stop-Loss kann sie früher schließen.
- **Standardwerte**:
  - `FastLength` = 100
  - `SlowLength` = 400
  - `StopLossPercent` = 2
  - `CandleType` = 1 Minute
- **Implementierungen**: C# und Python.
