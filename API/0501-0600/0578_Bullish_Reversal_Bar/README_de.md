# Bullische Umkehrstab-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie - Bullischer Umkehrstab. Geht long, wenn ein bullischer Umkehrstab unterhalb der Alligator-Linien gebildet wird und der Preis über das Stabhoch ausbricht. Optionale Filter können Awesome Oscillator und Market Facilitation Index Squat-Bars aktivieren.

Das Setup sucht nach einem neuen Tief, das in der oberen Hälfte der Kerze schließt, während sich der Trend bullisch wendet. Die Bestätigung kommt, wenn der Preis das Stabhoch überschreitet.

## Details

- **Einstiegskriterien**:
  - Long: `bullish reversal bar && close > confirmation level`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - Stop-Loss am Stabtief oder wenn Trend nach unten dreht
- **Stops**: Stabtief gespeichert in `_stopLoss`
- **Standardwerte**:
  - `EnableAo` = false
  - `EnableMfi` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: Alligator, Awesome Oscillator, Market Facilitation Index
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
