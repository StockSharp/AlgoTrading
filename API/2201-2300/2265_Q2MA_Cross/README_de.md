# Q2MA-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Q2MA-Kreuzungs-Strategie handelt auf Basis der Kreuzung geglätteter gleitender Durchschnitte, die auf den Schlusskurs- und Eröffnungskurspreisen der Kerzen aufgebaut sind. Eine Long-Position wird eröffnet, wenn der Schlusskurs-Durchschnitt nach einem vorherigen Überschreiten unter den Eröffnungskurs-Durchschnitt fällt, während eine Short-Position bei der umgekehrten Kreuzung eröffnet wird. Positionen werden geschlossen, wenn ein gegenteiliger Trend auftritt. Die Strategie verwendet auch Stop-Loss- und Take-Profit-Niveaus, die in Ticks gemessen werden.

## Details

- **Einstiegskriterien**: Kreuzung zwischen gleitenden Durchschnitten von Schluss- und Eröffnungspreisen
- **Long/Short**: beide Richtungen
- **Ausstiegskriterien**: entgegengesetzte Kreuzung oder Stop-Loss/Take-Profit
- **Stops**: ja
- **Standardwerte**:
  - `Length` = 8
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Volume` = 1
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `Invert` = false
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Moving Average
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: H4
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
