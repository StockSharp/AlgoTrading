# MA Volume Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
MA Volume kombiniert einen gleitenden Durchschnitt als Trendfilter mit Volumenspitzen für das Einstiegs-Timing.
Steigendes Volumen bei einem Preis über dem Durchschnitt signalisiert starke Akkumulation; fallendes Volumen unterhalb des Durchschnitts deutet auf Distribution hin.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 136%. Die Strategie funktioniert am besten auf dem Aktienmarkt.

Die Strategie handelt in Richtung des gleitenden Durchschnitts, wenn das Volumen zunimmt, und steigt aus, sobald das Volumen versiegt oder der Durchschnitt dreht.

Ein prozentualer Stop schützt vor plötzlichen Trendwenden.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Moving Average, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

