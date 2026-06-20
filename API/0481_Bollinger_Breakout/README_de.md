# 4H Bollinger Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die 4H Bollinger Ausbruch-Strategie handelt Bollinger-Band-Ausbrüche auf dem Vier-Stunden-Chart. Long-Positionen werden eröffnet, wenn der Preis über das untere Band kreuzt und Volumen sowie Trend bestätigen. Short-Positionen werden eingegangen, wenn der Preis unter das obere Band kreuzt und der RSI unter einem Schwellenwert liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs kreuzt über das untere Band, Volumen über seinem SMA und Preis über dem Trend-SMA.
  - **Short**: Schlusskurs kreuzt unter das obere Band, Volumen über seinem SMA, Preis unter dem Trend-SMA, RSI < 85.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Schlusskurs kreuzt über das obere Band.
  - **Short**: Schlusskurs kreuzt unter das untere Band.
- **Stops**: Keine.
- **Standardwerte**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 1.8
  - `VolumeLength` = 20
  - `TrendLength` = 80
  - `RsiLength` = 14
  - `UseLongSignals` = True
  - `UseShortSignals` = True
- **Filter**:
  - Kategorie: Trend-Ausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Volumen-SMA, Trend-SMA, RSI
  - Stops: Keine
  - Komplexität: Moderat
  - Zeitrahmen: 4H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
