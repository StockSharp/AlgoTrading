# XKRI Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie auf Basis des Kairi Relative Index (KRI), geglättet durch einen exponentiellen gleitenden Durchschnitt. Das System sucht nach lokalen Minima und Maxima des geglätteten Oszillators und eröffnet Long- oder Short-Positionen, wenn ein Umkehrmuster erscheint.

## Details

- **Einstiegskriterien**:
  - Long: `Kri[1] < Kri[2] && Kri[0] > Kri[1]`
  - Short: `Kri[1] > Kri[2] && Kri[0] < Kri[1]`
- **Long/Short**: Beide
- **Stops**: Take-Profit und Stop-Loss in Punkten
- **Standardwerte**:
  - `KriPeriod` = 20
  - `SmoothPeriod` = 7
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Kairi, EMA
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
