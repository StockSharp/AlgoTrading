# Umkehr-Fänger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Reversal Catcher steigt ein, wenn der Preis eine Bollinger-Band überschreitet und dann wieder eintritt, während sich der Momentum verschiebt. Er stützt sich auf einen schnellen und langsamen EMA zur Bestimmung der Trendrichtung und nutzt RSI-Kreuzungen von überkauften oder überverkauften Niveaus für das Timing der Einstiege. Ziele und Stops werden aus Bollinger-Band-Niveaus und dem Extrem der vorherigen Kerze abgeleitet. Positionen können optional zu einer festgelegten Tagesendzeit geschlossen werden.

## Details

- **Einstiegskriterien**: Preis kehrt in Bollinger Bands zurück mit höherer Hoch-/Tief-Struktur und RSI kreuzt Extremwerte.
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss, Ziel oder Tagesend-Flattening
- **Stops**: Extrem der vorherigen Kerze
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 1.5
  - `FastEmaPeriod` = 21
  - `SlowEmaPeriod` = 50
  - `RsiPeriod` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `EndOfDay` = 1500
  - `CandleType` = 5 Minuten
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, EMA, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
