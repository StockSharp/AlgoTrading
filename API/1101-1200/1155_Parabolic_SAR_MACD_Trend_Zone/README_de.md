# Parabolic SAR mit MACD-Bestätigung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den Parabolic SAR-Indikator mit einer MACD-Bestätigung. Eine Position wird eröffnet, wenn der Kurs den SAR in einer durch den MACD unterstützten Richtung kreuzt, um Trendumkehrungen zu erfassen.

## Details

- **Einstiegskriterien**: Kurs kreuzt den SAR und die MACD-Linie liegt auf derselben Seite ihrer Signallinie.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenläufige Kreuzung von Kurs/SAR oder MACD.
- **Stops**: Nein.
- **Standardwerte**:
  - `SarStart` = 0.02m
  - `SarIncrement` = 0.02m
  - `SarMax` = 0.2m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Parabolic SAR, MACD
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
