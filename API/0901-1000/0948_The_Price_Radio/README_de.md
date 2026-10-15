# Price Radio-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert den Price Radio-Indikator von John Ehlers. Sie geht long, wenn die Preisableitung sowohl den Amplituden- als auch den Frequenzschwellenwert überschreitet, und short, wenn sie unter deren negative Werte fällt.

## Details

- **Einstiegskriterien**:
  - **Long**: Ableitung ist größer als Amplitude und Frequenz.
  - **Short**: Ableitung ist kleiner als negative Amplitude und negative Frequenz.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 14.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Custom
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
