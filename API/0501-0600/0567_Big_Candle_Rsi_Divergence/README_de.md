# RSI-Divergenz bei großen Kerzen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Identifiziert ungewöhnlich große Kerzen im Vergleich zu den vorherigen fünf Bars und vergleicht schnelle und langsame RSI-Werte. Trades folgen der Kerzenrichtung und verwenden einen verzögerten Trailing-Stop, der erst aktiviert wird, nachdem sich der Preis eine festgelegte Anzahl von Ticks in den Gewinn bewegt hat.

Der Trailing-Stop beginnt, sobald der Gewinnschwellenwert erreicht ist, und verfolgt dann den Preis auf einem festen Abstand, während ein anfänglicher fester Stop den Trade von Beginn an schützt.

## Details

- **Einstiegskriterien**:
  - **Long**: Aktueller Kerzenkörper größer als die vorherigen fünf und Schlusskurs höher.
  - **Short**: Aktueller Kerzenkörper größer als die vorherigen fünf und Schlusskurs tiefer.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Anfänglicher Stop oder Trailing-Stop getroffen.
- **Stops**: Ja, verzögerter Trailing-Stop.
- **Standardwerte**:
  - `TrailStartTicks` = 200
  - `TrailDistanceTicks` = 150
  - `InitialStopLossTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
