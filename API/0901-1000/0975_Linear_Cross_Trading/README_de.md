# Lineare Kreuzhandel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet eine lineare Regression des Preises basierend auf dem Volumen, um einen vorhergesagten Preis zu erzeugen. Eine Long-Position wird eröffnet, wenn der vorhergesagte Preis über seinen gewichteten gleitenden Durchschnitt kreuzt und die MACD-Linie über ihr Signal steigt. Eine Short-Position wird eröffnet, wenn die MACD-Linie unter ihr Signal fällt und die jüngsten Tiefs rückläufig sind.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorhergesagter Preis kreuzt über seinen WMA und MACD steigt über das Signal.
  - **Short**: MACD fällt unter das Signal und Tiefs machen niedrigere Tiefs.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Keine; Positionen werden bei entgegengesetzten Signalen umgekehrt.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 21.
  - `LinearLength` = 9.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Linear Regression, WMA, MACD
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
