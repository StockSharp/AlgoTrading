# Bollinger-Bands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Bollinger-Bands-Ausbrüche handelt. Kauft, wenn der Preis über das obere Band schließt, und verkauft, wenn er unter das untere Band schließt. Steigt aus bei einem einfachen gleitenden Durchschnitt-Kreuz oder wenn der Stop Loss ausgelöst wird.

## Details

- **Einstiegskriterien**:
  - Long: Schlusskurs über dem oberen Bollinger Band
  - Short: Schlusskurs unter dem unteren Bollinger Band
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: Schlusskurs unter dem SMA oder Preis trifft Stop Loss
  - Short: Schlusskurs über dem SMA oder Preis trifft Stop Loss
- **Stops**: Prozentsatz vom Einstiegspreis
- **Standardwerte**:
  - `BbLength` = 120
  - `BbDeviation` = 2m
  - `SmaLength` = 110
  - `StopLossPercent` = 6m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
