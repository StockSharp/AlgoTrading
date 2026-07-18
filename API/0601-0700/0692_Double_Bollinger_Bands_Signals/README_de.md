# Doppelte Bollinger Bands Signale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet zwei Sätze von Bollinger Bands. Sie kauft, wenn der Preis das untere Band mit 3 Standardabweichungen nach oben durchbricht, und verkauft, wenn der Preis das obere Band mit 3 Standardabweichungen nach unten durchbricht. Positionen werden an den gegenüberliegenden Bändern mit 2 Standardabweichungen geschlossen.

## Details

- **Einstiegskriterien**:
  - Long: Schlusskurs durchbricht das untere 3-SD-Band nach oben
  - Short: Schlusskurs durchbricht das obere 3-SD-Band nach unten
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: Schlusskurs durchbricht das obere 2-SD-Band nach oben
  - Short: Schlusskurs durchbricht das untere 2-SD-Band nach unten
- **Stops**: Keine
- **Standardwerte**:
  - `Length` = 20
  - `Width1` = 2m
  - `Width2` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
