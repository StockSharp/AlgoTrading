# Follow-Line-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verfolgt eine Follow-Line, die aus Bollinger-Band-Ausbrüchen mit optionalem ATR-Offset abgeleitet wird. Einstiege erfolgen, wenn die Linie die Richtung ändert, optional bestätigt durch den Trend eines höheren Zeitrahmens.

## Details

- **Einstiegskriterien**: Follow-Line ändert die Richtung, nachdem der Preis die Bollinger Bands durchbricht, mit optionaler Bestätigung durch den höheren Zeitrahmen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Follow-Line oder Trend des höheren Zeitrahmens kehrt um.
- **Stops**: Nein.
- **Standardwerte**:
  - `AtrPeriod` = 5
  - `BbPeriod` = 21
  - `BbDeviation` = 1
  - `UseAtrFilter` = true
  - `UseTimeFilter` = false
  - `Session` = "0000-2400"
  - `UseHtfConfirmation` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HtfCandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, ATR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
